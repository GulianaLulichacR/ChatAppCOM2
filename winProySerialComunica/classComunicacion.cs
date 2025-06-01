using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winProySerialComunica
{
    class classComunicacion
    {
        public delegate void manejadorEventos(string mensaje);
        public delegate void manejadorArchivo(string fileName);
        public delegate void manejadorProgreso(string fileName, long enviado, long total);
        public delegate void manejadorProgresoRecepcion(string fileName, long recibido, long total);

        public event manejadorEventos LlegoMensaje;
        public event manejadorArchivo LlegoArchivo;
        public event manejadorProgreso ProgresoEnvio;
        public event manejadorProgresoRecepcion ProgresoRecepcion;

        private List<byte> bufferReensamblado = new List<byte>();
        private const int umbralFragmento = 1018;
        private readonly StringBuilder debugLog = new StringBuilder();
        private readonly object lockObject = new object();
        private readonly object serialWriteLock = new object(); // Lock específico para escritura serial

        private class ArchivoEnviando
        {
            public string Id { get; set; } // Identificador único
            public string Nombre { get; set; }
            public long Tamaño { get; set; }
            public long Avance { get; set; }
            public FileStream FlujoArchivo { get; set; }
            public BinaryReader LectorArchivo { get; set; }
            public BinaryWriter EscritorArchivo { get; set; }
            public CancellationTokenSource TokenCancelacion { get; set; }
        }

        private SerialPort sPuerto;
        private Thread procesoEnviaMensaje;
        private string mensajeEnviar;

        // Cambio: En lugar de cola secuencial, usamos lista de tareas paralelas
        private ConcurrentDictionary<string, Task> tareasEnvioArchivos;
        private ConcurrentDictionary<string, ArchivoEnviando> archivosEnviando;
        private ConcurrentDictionary<string, ArchivoEnviando> archivosRecibiendo;

        private volatile bool enviandoMensajes = false;
        private int contadorArchivos = 0; // Para generar IDs únicos

        private byte[] tramaInfo;
        private byte[] tramaCabezera;
        private byte[] tramaRelleno;
        private byte[] tramaRecibida;

        public classComunicacion(string nomPuerto, int velocidad)
        {
            try
            {
                sPuerto = new SerialPort();
                sPuerto.DataReceived += sPuerto_Datareceived;
                sPuerto.DataBits = 8;
                sPuerto.StopBits = StopBits.Two;
                sPuerto.Parity = Parity.Odd;
                sPuerto.ReadBufferSize = 8192;
                sPuerto.WriteBufferSize = 4096;
                sPuerto.ReceivedBytesThreshold = 1;
                sPuerto.ReadTimeout = 5000;
                sPuerto.WriteTimeout = 5000;

                sPuerto.PortName = nomPuerto;
                sPuerto.BaudRate = velocidad;
                sPuerto.Open();

                tramaInfo = new byte[1024];
                tramaCabezera = new byte[6];
                tramaRelleno = new byte[1024];
                tramaRecibida = new byte[1024];

                for (int i = 0; i <= 1023; i++)
                    tramaRelleno[i] = 64;

                // Inicializar estructuras concurrentes
                tareasEnvioArchivos = new ConcurrentDictionary<string, Task>();
                archivosEnviando = new ConcurrentDictionary<string, ArchivoEnviando>();
                archivosRecibiendo = new ConcurrentDictionary<string, ArchivoEnviando>();

                LogMessage("Se abrió el puerto sin problemas");
                MessageBox.Show("Se abrió el puerto sin problemas");
            }
            catch (Exception ex)
            {
                LogMessage($"Error al abrir puerto: {ex.Message}");
                MessageBox.Show("Error descripción: " + ex.Message);
            }
        }

        private void LogMessage(string message)
        {
            lock (lockObject)
            {
                debugLog.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                try
                {
                    File.AppendAllText("debug_log.txt", $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                }
                catch { }
            }
        }

        public void enviaMensaje(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                LogMessage("El mensaje no puede estar vacío.");
                MessageBox.Show("El mensaje no puede estar vacío.");
                return;
            }

            // Esperar si hay mensajes enviándose, pero no archivos
            while (enviandoMensajes)
            {
                Thread.Sleep(50);
            }

            mensajeEnviar = mensaje;
            procesoEnviaMensaje = new Thread(enviandoMensajeLargo) { IsBackground = true };
            procesoEnviaMensaje.Start();
        }

        private void enviandoMensajeLargo()
        {
            try
            {
                enviandoMensajes = true;
                lock (serialWriteLock) // Lock solo para escritura serial
                {
                    byte[] fullMensaje = Encoding.UTF8.GetBytes(mensajeEnviar);
                    int maxChunkSize = 1018;
                    int totalLength = fullMensaje.Length;
                    int offset = 0;

                    LogMessage($"Enviando mensaje de {totalLength} bytes");

                    while (offset < totalLength)
                    {
                        int chunkSize = Math.Min(maxChunkSize, totalLength - offset);
                        byte[] chunk = new byte[chunkSize];
                        Array.Copy(fullMensaje, offset, chunk, 0, chunkSize);

                        string cabecera = "M" + chunkSize.ToString("D5");
                        byte[] headerBytes = Encoding.ASCII.GetBytes(cabecera);

                        sPuerto.Write(headerBytes, 0, 6);
                        sPuerto.Write(chunk, 0, chunkSize);
                        if (chunkSize < maxChunkSize)
                            sPuerto.Write(tramaRelleno, 0, maxChunkSize - chunkSize);

                        offset += chunkSize;
                        LogMessage($"Enviado chunk de mensaje: {chunkSize} bytes, offset: {offset}/{totalLength}");
                        Thread.Sleep(50); // Reducido para mayor velocidad
                    }
                    LogMessage("Mensaje enviado completamente");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error al enviar mensaje largo: {ex.Message}");
                MessageBox.Show("Error al enviar mensaje largo: " + ex.Message);
            }
            finally
            {
                enviandoMensajes = false;
            }
        }


        public void IniciaEnvioArchivo(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception("El archivo no existe");
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    throw new Exception("El archivo está vacío");
                }

                // Use a simple counter-based ID
                string archivoId = $"F{Interlocked.Increment(ref contadorArchivos):D4}"; // e.g., F0001, F0002

                var archivoEnviar = new ArchivoEnviando
                {
                    Id = archivoId,
                    Nombre = Path.GetFileName(filePath),
                    Tamaño = fileInfo.Length,
                    Avance = 0,
                    FlujoArchivo = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                    TokenCancelacion = new CancellationTokenSource()
                };
                archivoEnviar.LectorArchivo = new BinaryReader(archivoEnviar.FlujoArchivo);

                archivosEnviando[archivoId] = archivoEnviar;

                var tareaEnvio = Task.Run(() => EnviarArchivoCompleto(archivoEnviar), archivoEnviar.TokenCancelacion.Token);
                tareasEnvioArchivos[archivoId] = tareaEnvio;

                LimpiarTareasCompletadas();

                LogMessage($"Iniciado envío paralelo de archivo: {archivoEnviar.Nombre} (ID: {archivoId}), tamaño: {archivoEnviar.Tamaño} bytes");
            }
            catch (Exception ex)
            {
                LogMessage($"Error al iniciar envío de archivo: {ex.Message}");
                MessageBox.Show($"Error al iniciar envío de archivo: {ex.Message}");
            }
        }


        private void LimpiarTareasCompletadas()
        {
            var tareasCompletadas = tareasEnvioArchivos.Where(kvp => kvp.Value.IsCompleted).ToList();
            foreach (var tarea in tareasCompletadas)
            {
                Task tareaRemovida;

                tareasEnvioArchivos.TryRemove(tarea.Key, out tareaRemovida);
                ArchivoEnviando archivo;

                archivosEnviando.TryRemove(tarea.Key, out archivo);
                archivo?.TokenCancelacion?.Dispose();
            }
        }



        private async Task EnviarArchivoCompleto(ArchivoEnviando archivoEnviar)
        {
            try
            {
                LogMessage($"Iniciando envío de archivo: {archivoEnviar.Nombre} (ID: {archivoEnviar.Id})");

                string safeFileName = archivoEnviar.Nombre.Replace("|", "_").Replace("\0", "");
                if (string.IsNullOrEmpty(safeFileName) || safeFileName.Length > 200)
                {
                    throw new Exception($"Nombre de archivo inválido: {safeFileName}");
                }

                // 1. Enviar metadatos del archivo con ID único
                string fileInfo = $"{archivoEnviar.Id}|{safeFileName}|{archivoEnviar.Tamaño}";
                byte[] fileInfoBytes = Encoding.UTF8.GetBytes(fileInfo);

                if (fileInfoBytes.Length > 1018)
                {
                    throw new Exception($"Metadatos demasiado largos: {fileInfoBytes.Length} bytes");
                }

                // Enviar metadatos de forma sincronizada
                lock (serialWriteLock)
                {
                    string cabecera = "F" + fileInfoBytes.Length.ToString("D5");
                    byte[] headerBytes = Encoding.ASCII.GetBytes(cabecera);

                    LogMessage($"Enviando metadatos: {fileInfo}");
                    sPuerto.Write(headerBytes, 0, 6);
                    sPuerto.Write(fileInfoBytes, 0, fileInfoBytes.Length);
                    sPuerto.Write(tramaRelleno, 0, 1018 - fileInfoBytes.Length);
                }

                await Task.Delay(200); // Pausa asíncrona

                // 2. Enviar contenido del archivo
                byte[] buffer = new byte[1018];
                long totalBytesSent = 0;

                LogMessage($"Iniciando envío de contenido, tamaño total: {archivoEnviar.Tamaño} bytes");

                while (archivoEnviar.Avance < archivoEnviar.Tamaño && !archivoEnviar.TokenCancelacion.Token.IsCancellationRequested)
                {
                    long bytesRestantes = archivoEnviar.Tamaño - archivoEnviar.Avance;
                    int bytesToRead = (int)Math.Min(1018, bytesRestantes);

                    int bytesRead = archivoEnviar.LectorArchivo.Read(buffer, 0, bytesToRead);
                    if (bytesRead <= 0)
                    {
                        LogMessage($"Error: No se pudieron leer bytes del archivo. Esperados: {bytesToRead}, Leídos: {bytesRead}");
                        break;
                    }

                    archivoEnviar.Avance += bytesRead;
                    totalBytesSent += bytesRead;

                    // CORRECCIÓN: Cambiar el formato de la cabecera de datos
                    lock (serialWriteLock)
                    {
                        string dataCabecera = $"A{archivoEnviar.Id}{bytesRead:X2}"; // Use full ID (F0001)
                        byte[] dataHeaderBytes = Encoding.ASCII.GetBytes(dataCabecera);

                        sPuerto.Write(dataHeaderBytes, 0, 6);
                        sPuerto.Write(buffer, 0, bytesRead);

                        if (bytesRead < 1018)
                        {
                            sPuerto.Write(tramaRelleno, 0, 1018 - bytesRead);
                        }
                    }

                    // Notificar progreso en tiempo real
                    ProgresoEnvio?.Invoke(safeFileName, totalBytesSent, archivoEnviar.Tamaño);

                    LogMessage($"[{archivoEnviar.Id}] Enviado chunk: {bytesRead} bytes, total: {totalBytesSent}/{archivoEnviar.Tamaño} ({(totalBytesSent * 100 / archivoEnviar.Tamaño):F1}%)");

                    await Task.Delay(100); // Pausa asíncrona para permitir otros archivos
                }

                archivoEnviar.LectorArchivo?.Close();
                archivoEnviar.FlujoArchivo?.Close();

                LogMessage($"Archivo {safeFileName} enviado completamente. Total bytes: {totalBytesSent}");

                if (Application.OpenForms.Count > 0)
                {
                    Application.OpenForms[0].Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Archivo {safeFileName} enviado completamente");
                    }));
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error al enviar archivo {archivoEnviar?.Nombre}: {ex.Message}");

                try
                {
                    archivoEnviar?.LectorArchivo?.Close();
                    archivoEnviar?.FlujoArchivo?.Close();
                }
                catch { }

                if (Application.OpenForms.Count > 0)
                {
                    Application.OpenForms[0].Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Error al enviar archivo: {ex.Message}");
                    }));
                }
            }
        }



        public void sPuerto_Datareceived(object o, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (sPuerto.BytesToRead >= 6)
                {
                    byte[] headerBuffer = new byte[6];
                    int bytesRead = sPuerto.Read(headerBuffer, 0, 6);
                    if (bytesRead != 6)
                    {
                        LogMessage($"Lectura incompleta de cabecera: {bytesRead}/6 bytes, Bytes: {BitConverter.ToString(headerBuffer, 0, bytesRead)}");
                        continue;
                    }

                    string cabecera = Encoding.ASCII.GetString(headerBuffer, 0, 6);
                    LogMessage($"Recibida cabecera: '{cabecera}' ({BitConverter.ToString(headerBuffer)})");

                    if (cabecera.Length != 6)
                    {
                        LogMessage($"Cabecera inválida: '{cabecera}'");
                        continue;
                    }

                    int totalBytesToRead = 1018;
                    DateTime startTime = DateTime.Now;
                    while (sPuerto.BytesToRead < totalBytesToRead && (DateTime.Now - startTime).TotalMilliseconds < 3000)
                    {
                        Thread.Sleep(5);
                    }

                    if (sPuerto.BytesToRead < totalBytesToRead)
                    {
                        LogMessage($"Timeout esperando datos. Disponibles: {sPuerto.BytesToRead}, Esperados: {totalBytesToRead}, Cabecera: {cabecera}");
                        continue;
                    }

                    byte[] dataBuffer = new byte[1018];
                    bytesRead = sPuerto.Read(dataBuffer, 0, 1018);
                    if (bytesRead != 1018)
                    {
                        LogMessage($"Lectura incompleta de datos: {bytesRead}/1018 bytes, Cabecera: {cabecera}");
                        continue;
                    }

                    LogMessage($"Recibida trama: '{cabecera}'");

                    if (cabecera[0] == 'M')
                    {
                        int longitud = int.Parse(cabecera.Substring(1, 5));
                        ProcesarMensaje(dataBuffer, longitud);
                    }
                    else if (cabecera[0] == 'F')
                    {
                        int longitud = int.Parse(cabecera.Substring(1, 5));
                        ProcesarMetadatosArchivo(dataBuffer, longitud);
                    }
                    else if (cabecera[0] == 'A')
                    {

                        string archivoIdParcial = cabecera.Substring(1, 4); // e.g., F0001
                        int longitud = Convert.ToInt32(cabecera.Substring(5, 2), 16);
                        MessageBox.Show(archivoIdParcial , Convert.ToString(longitud)  );
                        MessageBox.Show(dataBuffer);

                        ProcesarDatosArchivo(dataBuffer, longitud, archivoIdParcial);
                    }
                    else
                    {
                        LogMessage($"Tipo de trama no reconocido: '{cabecera[0]}'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error en recepción de datos: {ex.Message}");
            }
        }


        private void ProcesarMensaje(byte[] datos, int longitud)
        {
            try
            {
                byte[] fragmento = new byte[longitud];
                Array.Copy(datos, 0, fragmento, 0, longitud);
                bufferReensamblado.AddRange(fragmento);

                LogMessage($"Fragmento de mensaje recibido: {longitud} bytes");

                if (longitud < umbralFragmento)
                {
                    string mensajeCompleto = Encoding.UTF8.GetString(bufferReensamblado.ToArray());
                    bufferReensamblado.Clear();
                    LogMessage($"Mensaje completo recibido: {mensajeCompleto.Length} caracteres");
                    onLLegoMensaje(mensajeCompleto);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error procesando mensaje: {ex.Message}");
                bufferReensamblado.Clear();
            }
        }




        private void ProcesarMetadatosArchivo(byte[] datos, int longitud)
        {
            try
            {
                string content = Encoding.UTF8.GetString(datos, 0, longitud);
                LogMessage($"Procesando metadatos de archivo: {content}");

                string[] parts = content.Split('|');
                if (parts.Length != 3)
                {
                    LogMessage($"Formato de metadatos inválido: {content}, partes: {parts.Length}");
                    return;
                }

                string archivoId = parts[0].Trim();
                string fileName = parts[1].Trim();
                long fileSize;

                if (!long.TryParse(parts[2].Trim(), out fileSize) || fileSize <= 0 || string.IsNullOrEmpty(fileName))
                {
                    LogMessage($"Metadatos inválidos: ID={parts[0]}, Nombre={parts[1]}, Tamaño={parts[2]}");
                    return;
                }

                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
                LogMessage($"Metadatos válidos: ID={archivoId}, Nombre={fileName}, Tamaño={fileSize}, Ruta={downloadsPath}");

                // Check if file already exists in archivosRecibiendo
                if (archivosRecibiendo.ContainsKey(archivoId))
                {
                    LogMessage($"Archivo con ID {archivoId} ya está en recepción, ignorando metadatos");
                    return;
                }

                InicioConstruirArchivo(downloadsPath, fileSize, archivoId, fileName);
                ProgresoRecepcion?.Invoke(fileName, 0, fileSize);
            }
            catch (Exception ex)
            {
                LogMessage($"Error procesando metadatos de archivo: {ex.Message}");
            }
        }



        private void ProcesarDatosArchivo(byte[] datos, int longitud, string archivoIdParcial)
        {
            try
            {
                MessageBox.Show("Prueba2");

                ArchivoEnviando archivo = null;
                if (archivosRecibiendo.TryGetValue(archivoIdParcial, out archivo))
                {
                    LogMessage($"Encontrado archivo para ID: {archivoIdParcial}, escribiendo {longitud} bytes");
                    ConstruirArchivo(archivo.Id, longitud, datos);
                }
                else
                {
                    LogMessage($"No se encontró archivo para ID: {archivoIdParcial}, archivos en recepción: {string.Join(", ", archivosRecibiendo.Keys)}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error procesando datos de archivo (ID: {archivoIdParcial}): {ex.Message}");
            }
        }

        public void InicioConstruirArchivo(string rutaCompleta, long tamaño, string archivoId, string fileName)
        {
            try
            {
                string directorio = Path.GetDirectoryName(rutaCompleta);
                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                    LogMessage($"Creado directorio: {directorio}");
                }

                if (File.Exists(rutaCompleta))
                {
                    try
                    {
                        using (var fs = File.Open(rutaCompleta, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            fs.Close();
                        }
                        File.Delete(rutaCompleta);
                        LogMessage($"Archivo existente {rutaCompleta} eliminado para nueva recepción");
                    }
                    catch
                    {
                        LogMessage($"No se pudo eliminar archivo existente {rutaCompleta}, puede estar en uso");
                    }
                }

                var archivo = new ArchivoEnviando
                {
                    Id = archivoId,
                    Nombre = rutaCompleta,
                    Tamaño = tamaño,
                    Avance = 0,
                    FlujoArchivo = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write, FileShare.None),
                    EscritorArchivo = null
                };
                archivo.EscritorArchivo = new BinaryWriter(archivo.FlujoArchivo);
                archivosRecibiendo[archivoId] = archivo;
                LogMessage($"Archivo preparado para recepción: {fileName} -> {rutaCompleta} (ID: {archivoId}), Tamaño esperado: {tamaño}");

                // Verify stream is writable
                if (!archivo.FlujoArchivo.CanWrite)
                {
                    LogMessage($"Error: FileStream para {rutaCompleta} no es escribible");
                    throw new IOException("FileStream no es escribible");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error preparando archivo {rutaCompleta}: {ex.Message}");
            }
        }

        private void ConstruirArchivo(string archivoId, int longitud, byte[] datos)
        {
            try
            {
                MessageBox.Show("Prueba");

                ArchivoEnviando archivo;
                if (archivosRecibiendo.TryGetValue(archivoId, out archivo))
                {
                    long bytesRestantes = archivo.Tamaño - archivo.Avance;
                    int bytesToWrite = Math.Min(longitud, (int)bytesRestantes);

                    if (bytesToWrite > 0)
                    {
                        archivo.EscritorArchivo.Write(datos, 0, bytesToWrite);
                        archivo.EscritorArchivo.Flush(); // Ensure immediate write
                        archivo.FlujoArchivo.Flush();   // Ensure stream is flushed
                        archivo.Avance += bytesToWrite;

                        string fileName = Path.GetFileName(archivo.Nombre);
                        double porcentaje = (double)archivo.Avance * 100 / archivo.Tamaño;

                        LogMessage($"[{archivoId}] Escritos {bytesToWrite} bytes a {fileName}, progreso: {archivo.Avance}/{archivo.Tamaño} ({porcentaje:F1}%)");

                        // Notificar progreso de recepción en tiempo real
                        ProgresoRecepcion?.Invoke(fileName, archivo.Avance, archivo.Tamaño);

                        if (archivo.Avance >= archivo.Tamaño)
                        {
                            archivo.EscritorArchivo.Close();
                            archivo.EscritorArchivo.Dispose();
                            archivo.FlujoArchivo.Close();
                            archivo.FlujoArchivo.Dispose();

                            ArchivoEnviando archivoRemovido;
                            archivosRecibiendo.TryRemove(archivoId, out archivoRemovido);

                            LogMessage($"Archivo recibido completamente: {fileName} (ID: {archivoId}), Tamaño final: {new FileInfo(archivo.Nombre).Length} bytes");
                            onLlegoArchivo(archivo.Nombre);
                        }
                    }
                }
                else
                {
                    LogMessage($"No se encontró archivo en recepción: {archivoId}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error construyendo archivo {archivoId}: {ex.Message}");
                classComunicacion.ArchivoEnviando archivo;
                if (archivosRecibiendo.TryGetValue(archivoId, out archivo))
                {
                    try
                    {
                        archivo.EscritorArchivo?.Close();
                        archivo.EscritorArchivo?.Dispose();
                        archivo.FlujoArchivo?.Close();
                        archivo.FlujoArchivo?.Dispose();
                        ArchivoEnviando archivoRemovido;
                        archivosRecibiendo.TryRemove(archivoId, out archivoRemovido);
                    }
                    catch { }
                }
            }
        }

        protected virtual void onLLegoMensaje(string mens)
        {
            LlegoMensaje?.Invoke(mens);
        }

        protected virtual void onLlegoArchivo(string fileName)
        {
            LlegoArchivo?.Invoke(fileName);
        }

        public void CancelarEnvioArchivo(string fileName)
        {
            var archivo = archivosEnviando.Values.FirstOrDefault(a => a.Nombre == fileName);
            if (archivo != null)
            {
                archivo.TokenCancelacion.Cancel();
                LogMessage($"Cancelado envío de archivo: {fileName}");
            }
        }

        public void Cerrar()
        {
            try
            {
                // Cancelar todas las tareas de envío
                foreach (var archivo in archivosEnviando.Values)
                {
                    archivo.TokenCancelacion?.Cancel();
                    archivo.LectorArchivo?.Close();
                    archivo.FlujoArchivo?.Close();
                }

                // Cerrar archivos en recepción
                foreach (var archivo in archivosRecibiendo.Values)
                {
                    archivo.EscritorArchivo?.Close();
                    archivo.FlujoArchivo?.Close();
                }

                archivosEnviando.Clear();
                archivosRecibiendo.Clear();
                tareasEnvioArchivos.Clear();

                if (sPuerto?.IsOpen == true)
                {
                    sPuerto.Close();
                }

                procesoEnviaMensaje?.Abort();
            }
            catch (Exception ex)
            {
                LogMessage($"Error cerrando comunicación: {ex.Message}");
            }
        }
    }
}
