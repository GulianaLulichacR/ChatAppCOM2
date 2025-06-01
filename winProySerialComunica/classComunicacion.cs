using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace winProySerialComunica
{
    class classComunicacion
    {
        public delegate void manejadorEventos(string mensaje);
        public delegate void manejadorArchivo(string fileName);
        public event manejadorEventos LlegoMensaje;
        public event manejadorArchivo LlegoArchivo;

        private List<byte> bufferReensamblado = new List<byte>();
        private const int umbralFragmento = 1018;

        private class ArchivoEnviando
        {
            public string Nombre { get; set; }
            public long Tamaño { get; set; }
            public long Avance { get; set; }
            public int Num { get; set; }
        }

        private SerialPort sPuerto;
        private Thread procesoEnviaMensaje;
        private Thread procesoVerificaSalida;
        private Thread procesoEnvioArchivo;
        private Thread procesoConstruyeArchivo;
        private string mensajeEnviar;
        private ArchivoEnviando archivoEnviar;
        private ArchivoEnviando archivoRecibir;
        private FileStream flujoArchivoEnviar;
        private BinaryReader leyendoArchivo;
        private FileStream flujoArchivoRecibir;
        private BinaryWriter escribiendoArchivo;
        private bool bufferSalidaVacio;
        private bool ackReceived;

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
                sPuerto.ReadBufferSize = 2048;
                sPuerto.WriteBufferSize = 1024;
                sPuerto.ReceivedBytesThreshold = 6;

                sPuerto.PortName = nomPuerto;
                sPuerto.BaudRate = velocidad;
                sPuerto.Open();

                tramaInfo = new byte[1024];
                tramaCabezera = new byte[6];
                tramaRelleno = new byte[1024];
                tramaRecibida = new byte[1024];

                for (int i = 0; i <= 1023; i++)
                    tramaRelleno[i] = 64;

                archivoEnviar = new ArchivoEnviando();
                archivoRecibir = new ArchivoEnviando();
                bufferSalidaVacio = true;
                ackReceived = false;

                procesoVerificaSalida = new Thread(VerificandoSalida);
                procesoVerificaSalida.Start();

                MessageBox.Show("Se abrió el puerto sin problemas");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error descripción: " + ex.Message);
            }
        }

        public void enviaMensaje(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                MessageBox.Show("El mensaje no puede estar vacío.");
                return;
            }

            mensajeEnviar = mensaje;
            procesoEnviaMensaje = new Thread(enviandoMensajeLargo);
            procesoEnviaMensaje.Start();
        }

        private void enviandoMensajeLargo()
        {
            try
            {
                byte[] fullMensaje = Encoding.UTF8.GetBytes(mensajeEnviar);
                int maxChunkSize = 1018;
                int totalLength = fullMensaje.Length;
                int offset = 0;

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

                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar mensaje largo: " + ex.Message);
            }
        }



        public void sPuerto_Datareceived(object o, SerialDataReceivedEventArgs e)
        {
            while (sPuerto.BytesToRead >= 6)
            {
                byte[] headerBuffer = new byte[6];
                sPuerto.Read(headerBuffer, 0, 6);

                string cabecera;
                try
                {
                    cabecera = Encoding.ASCII.GetString(headerBuffer, 0, 6);
                    if (cabecera.Length != 6 || !cabecera.All(c => char.IsLetterOrDigit(c)))
                    {
                        MessageBox.Show($"Error: Cabecera inválida o corrupta. Contenido: '{cabecera}'");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer cabecera: {ex.Message}");
                    return;
                }

                string tipo = cabecera.Length >= 3 && cabecera.Substring(0, 3) == "ACK" ? "ACK" : cabecera.Substring(0, 2);
                string lengthStr = tipo == "ACK" ? "0000" : cabecera.Substring(2, 4);

                int longitud = 0;
                if (tipo != "ACK")
                {
                    if (!int.TryParse(lengthStr, out longitud) || longitud < 0 || longitud > 1018)
                    {
                        MessageBox.Show($"Error: Longitud inválida en cabecera '{cabecera}'. Valor extraído: '{lengthStr}'. Límite máximo: 1018");
                        return;
                    }
                }

                int bytesToRead = 0;
                if (tipo != "ACK")
                {
                    bytesToRead = longitud + (1018 - longitud);
                    if (sPuerto.BytesToRead < bytesToRead)
                    {
                        Thread.Sleep(50);
                        if (sPuerto.BytesToRead < bytesToRead)
                        {
                            MessageBox.Show($"Error: Datos incompletos, esperados {bytesToRead}, recibidos {sPuerto.BytesToRead}");
                            return;
                        }
                    }
                    sPuerto.Read(tramaRecibida, 0, bytesToRead);
                }

                switch (tipo)
                {
                    case "ACK":
                        ackReceived = true;
                        break;

                    case "M":
                        byte[] fragmento = new byte[longitud];
                        Array.Copy(tramaRecibida, 0, fragmento, 0, longitud);
                        bufferReensamblado.AddRange(fragmento);

                        if (longitud < umbralFragmento)
                        {
                            string mensajeCompleto = Encoding.UTF8.GetString(bufferReensamblado.ToArray());
                            bufferReensamblado.Clear();
                            onLLegoMensaje(mensajeCompleto);
                        }
                        sPuerto.Write(Encoding.ASCII.GetBytes("ACK000"), 0, 6);
                        break;

                    case "AC": // File metadata
                        string fileInfo = Encoding.UTF8.GetString(tramaRecibida, 0, longitud);
                        string[] parts = fileInfo.Split('|');
                        if (parts.Length == 2)
                        {
                            string fileName = parts[0];
                            long fileSize;
                            if (!long.TryParse(parts[1], out fileSize) || fileSize <= 0)
                            {
                                MessageBox.Show($"Error: Tamaño de archivo inválido en '{fileInfo}'. Valor: '{parts[1]}'");
                                return;
                            }
                            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
                            InicioConstruirArchivo(downloadsPath, fileSize, 1);
                        }
                        else
                        {
                            MessageBox.Show($"Error: Formato de metadatos inválido: '{fileInfo}'");
                        }
                        sPuerto.Write(Encoding.ASCII.GetBytes("ACK000"), 0, 6);
                        break;

                    case "AI": // File data chunk
                        procesoConstruyeArchivo = new Thread(() => ConstruirArchivo(longitud));
                        procesoConstruyeArchivo.Start();
                        sPuerto.Write(Encoding.ASCII.GetBytes("ACK000"), 0, 6);
                        break;

                    default:
                        MessageBox.Show($"Trama no reconocida: '{cabecera}'");
                        break;
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

        private void VerificandoSalida()
        {
            while (true)
            {
                bufferSalidaVacio = sPuerto.BytesToWrite == 0;
                Thread.Sleep(10);
            }
        }

        public void IniciaEnvioArchivo(string filePath)
        {
            try
            {
                flujoArchivoEnviar = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                leyendoArchivo = new BinaryReader(flujoArchivoEnviar);

                archivoEnviar.Nombre = Path.GetFileName(filePath);
                archivoEnviar.Tamaño = flujoArchivoEnviar.Length;
                archivoEnviar.Avance = 0;
                archivoEnviar.Num = 1;

                string fileInfo = $"{archivoEnviar.Nombre}|{archivoEnviar.Tamaño}";
                byte[] fileInfoBytes = Encoding.UTF8.GetBytes(fileInfo);
                string cabecera = "AC" + fileInfoBytes.Length.ToString("D4");
                byte[] headerBytes = Encoding.ASCII.GetBytes(cabecera);

                sPuerto.Write(headerBytes, 0, 6);
                sPuerto.Write(fileInfoBytes, 0, fileInfoBytes.Length);
                sPuerto.Write(tramaRelleno, 0, 1018 - fileInfoBytes.Length);

                ackReceived = false;
                int timeout = 5000;
                int elapsed = 0;
                while (!ackReceived && elapsed < timeout)
                {
                    Thread.Sleep(100);
                    elapsed += 100;
                }
                if (!ackReceived)
                {
                    MessageBox.Show("Error: No se recibió ACK para metadatos del archivo");
                    flujoArchivoEnviar.Close();
                    return;
                }

                procesoEnvioArchivo = new Thread(EnviandoArchivo);
                procesoEnvioArchivo.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar envío de archivo: {ex.Message}");
            }
        }

        private void EnviandoArchivo()
        {
            try
            {
                byte[] tramaEnvioArchivo = new byte[1018];
                string cabecera = "AI" + 1018.ToString("D4");
                byte[] headerBytes = Encoding.ASCII.GetBytes(cabecera);

                while (archivoEnviar.Avance <= archivoEnviar.Tamaño - 1018)
                {
                    leyendoArchivo.Read(tramaEnvioArchivo, 0, 1018);
                    archivoEnviar.Avance += 1018;

                    while (!bufferSalidaVacio) { Thread.Sleep(10); }

                    sPuerto.Write(headerBytes, 0, 6);
                    sPuerto.Write(tramaEnvioArchivo, 0, 1018);

                    ackReceived = false;
                    int timeout = 10000; // 10 seconds
                    int elapsed = 0;
                    while (!ackReceived && elapsed < timeout)
                    {
                        Thread.Sleep(100);
                        elapsed += 100;
                    }
                    if (!ackReceived)
                    {
                        MessageBox.Show($"Error: No se recibió ACK para chunk en avance {archivoEnviar.Avance}");
                        break;
                    }
                    else
                    {
                        // MessageBox.Show($"ACK recibido para chunk en avance {archivoEnviar.Avance}");

                    }
                }

                int tamanito = (int)(archivoEnviar.Tamaño - archivoEnviar.Avance);
                if (tamanito > 0)
                {
                    if (tamanito > 1018)
                    {
                        MessageBox.Show($"Error: Tamaño de chunk final ({tamanito}) excede el límite de 1018 bytes");
                        return;
                    }
                    tramaEnvioArchivo = new byte[tamanito];
                    leyendoArchivo.Read(tramaEnvioArchivo, 0, tamanito);
                    cabecera = "AI" + tamanito.ToString("D4");
                    headerBytes = Encoding.ASCII.GetBytes(cabecera);

                    while (!bufferSalidaVacio) { Thread.Sleep(10); }

                    sPuerto.Write(headerBytes, 0, 6);
                    sPuerto.Write(tramaEnvioArchivo, 0, tamanito);
                    sPuerto.Write(tramaRelleno, 0, 1018 - tamanito);

                    ackReceived = false;
                    int timeout = 10000; // 10 seconds
                    int elapsed = 0;
                    while (!ackReceived && elapsed < timeout)
                    {
                        Thread.Sleep(100);
                        elapsed += 100;
                    }
                    if (!ackReceived)
                    {
                        MessageBox.Show($"Error: No se recibió ACK para chunk final de {tamanito} bytes");
                    }
                    else
                    {
                        MessageBox.Show($"ACK recibido para chunk final de {tamanito} bytes");
                    }
                }

                leyendoArchivo.Close();
                flujoArchivoEnviar.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar archivo: {ex.Message}");
            }
        }




        public void InicioConstruirArchivo(string nombre, long tama, int idNum)
        {
            try
            {
                flujoArchivoRecibir = new FileStream(nombre, FileMode.Create, FileAccess.Write);
                escribiendoArchivo = new BinaryWriter(flujoArchivoRecibir);
                archivoRecibir.Nombre = nombre;
                archivoRecibir.Num = idNum;
                archivoRecibir.Tamaño = tama;
                archivoRecibir.Avance = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar recepción de archivo: {ex.Message}");
            }
        }

        public void InicioConstruirArchivo()
        {
            // Placeholder to signal readiness for file reception
            // Actual file creation is triggered by "AC" header in sPuerto_Datareceived
        }

        private void ConstruirArchivo(int longitud)
        {
            try
            {
                if (archivoRecibir.Avance < archivoRecibir.Tamaño)
                {
                    int bytesToWrite = Math.Min(longitud, (int)(archivoRecibir.Tamaño - archivoRecibir.Avance));
                    escribiendoArchivo.Write(tramaRecibida, 0, bytesToWrite);
                    archivoRecibir.Avance += bytesToWrite;

                    if (archivoRecibir.Avance >= archivoRecibir.Tamaño)
                    {
                        escribiendoArchivo.Close();
                        flujoArchivoRecibir.Close();
                        onLlegoArchivo(archivoRecibir.Nombre);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al construir archivo: {ex.Message}");
            }
        }
    }
}