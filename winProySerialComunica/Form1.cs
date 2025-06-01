using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winProySerialComunica
{
    public partial class FormChatApp : Form
    {
        classComunicacion ComunicaTXRX;
        private string nombreUsuarioLocal = "Guliana";
        private string nombreUsuarioRemoto = "Fernando";
        private Dictionary<string, string> enlacesArchivos = new Dictionary<string, string>();

        public FormChatApp()
        {
            InitializeComponent();

            rtxBox.LinkClicked += rtxBox_LinkClicked;

            ComunicaTXRX = new classComunicacion("COM1", 115200);
            ComunicaTXRX.LlegoMensaje += ComunicaTXRX_LLegomensaje;
            ComunicaTXRX.LlegoArchivo += ComunicaTXRX_LlegoArchivo; // New event for file reception

            rtxMensajeEnvia.KeyDown += rtxMensajeEnvia_KeyDown;

            btnCerrar.FlatStyle = FlatStyle.Flat;
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Text = "❌";
            btnCerrar.Click += btnCerrar_Click;

            btnMinimizar.FlatStyle = FlatStyle.Flat;
            btnMinimizar.FlatAppearance.BorderSize = 0;
            btnMinimizar.Text = "―";
            btnMinimizar.Click += btnMinimizar_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Panel panelSuperior = new Panel();
            panelSuperior.Name = "panelSuperior";
            panelSuperior.Height = 30;
            panelSuperior.Dock = DockStyle.Top;
            panelSuperior.BackColor = Color.Teal;
            this.Controls.Add(panelSuperior);
            panelSuperior.BackgroundImage = Image.FromFile(Application.StartupPath + @"\img\fondo.png");

            btnEnviaMensaje.Image = Image.FromFile(Application.StartupPath + @"\img\enviar.png");
            btnEnviarArchivo.Image = Image.FromFile(Application.StartupPath + @"\img\enviarArchivo.png");

            btnEnviaMensaje.Enabled = false;

            rtxBox.BorderStyle = BorderStyle.None;
            rtxBox.BackColor = Color.WhiteSmoke;
            rtxBox.Font = new Font("Segoe UI Emoji", 12);
            rtxBox.ForeColor = Color.Black;

            btnEnviaMensaje.FlatStyle = FlatStyle.Flat;
            btnEnviaMensaje.FlatAppearance.BorderSize = 0;
            btnEnviaMensaje.FlatAppearance.MouseOverBackColor = Color.DarkCyan;
            btnEnviaMensaje.FlatAppearance.MouseDownBackColor = Color.Black;

            rtxMensajeEnvia.BorderStyle = BorderStyle.None;
            rtxMensajeEnvia.BackColor = Color.White;
            rtxMensajeEnvia.Font = new Font("Segoe UI", 11);
            rtxMensajeEnvia.ForeColor = Color.Black;

            this.Shown += (s, ev) => RedondearTextBox(rtxMensajeEnvia);

            rtxMensajeEnvia.Focus();

            this.Shown += (s, ev) =>
            {
                RedondearTextBox(rtxMensajeEnvia);
                RedondearBoton(btnEnviaMensaje);
            };
        }

        private void AgregarMensaje(string remitente, string mensaje, bool alineadoDerecha)
        {
            string hora = DateTime.Now.ToString("HH:mm");

            rtxBox.SelectionStart = rtxBox.TextLength;
            rtxBox.SelectionLength = 0;
            rtxBox.SelectionAlignment = alineadoDerecha ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 10, FontStyle.Bold);
            rtxBox.SelectionColor = Color.DarkCyan;
            rtxBox.AppendText(remitente + ": ");

            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 12, FontStyle.Regular);
            rtxBox.SelectionColor = Color.Black;
            rtxBox.AppendText(mensaje + Environment.NewLine);

            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 8, FontStyle.Regular);
            rtxBox.SelectionColor = Color.Gray;
            rtxBox.AppendText("   " + hora + Environment.NewLine + Environment.NewLine);

            rtxBox.ScrollToCaret();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string mensaje = rtxMensajeEnvia.Text.Trim();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                ComunicaTXRX.enviaMensaje(mensaje);
                AgregarMensaje(nombreUsuarioLocal, mensaje, true);
                rtxMensajeEnvia.Clear();
                rtxMensajeEnvia.Focus();
            }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ComunicaTXRX_LLegomensaje(string m)
        {
            if (rtxBox.InvokeRequired)
            {
                rtxBox.Invoke(new Action(() =>
                {
                    AgregarMensaje(nombreUsuarioRemoto, m, false);
                }));
            }
            else
            {
                AgregarMensaje(nombreUsuarioRemoto, m, false);
            }
        }

        private void ComunicaTXRX_LlegoArchivo(string fileName)
        {
            if (rtxBox.InvokeRequired)
            {
                rtxBox.Invoke(new Action(() =>
                {
                    MostrarArchivoEnChat(nombreUsuarioRemoto, fileName, false);

                }));
            }
            else
            {
                MostrarArchivoEnChat(nombreUsuarioRemoto, fileName, false);

            }
        }

        private void MostrarArchivoEnChat(string remitente, string filePath, bool alineadoDerecha)
        {
            FileInfo fi = new FileInfo(filePath);
            string ext = fi.Extension.ToLower();
            string icono = "📄";
            if (ext == ".pdf") icono = "📕";
            else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png") icono = "🖼️";
            else if (ext == ".doc" || ext == ".docx") icono = "📄";


            string peso = (fi.Length / 1024.0).ToString("0.0") + " KB";
            string tipo = ext.Length > 1 ? ext.Substring(1).ToUpper() : "ARCHIVO";

            // Muestra remitente y hora
            string hora = DateTime.Now.ToString("HH:mm");
            rtxBox.SelectionStart = rtxBox.TextLength;
            rtxBox.SelectionLength = 0;
            rtxBox.SelectionAlignment = alineadoDerecha ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 10, FontStyle.Bold);
            rtxBox.SelectionColor = Color.DarkCyan;
            rtxBox.AppendText(remitente + ": ");

            // Muestra icono y nombre como enlace
            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 12, FontStyle.Regular);
            rtxBox.SelectionColor = Color.Black;
            rtxBox.AppendText(icono + " ");
            int linkStart = rtxBox.TextLength;
            rtxBox.SelectionStart = rtxBox.TextLength;
            rtxBox.SelectionLength = 0;
            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 12, FontStyle.Underline);
            rtxBox.SelectionColor = Color.Blue;
            rtxBox.AppendText(fi.Name);
            rtxBox.SelectionStart = rtxBox.TextLength;
            rtxBox.SelectionLength = 0;
            rtxBox.AppendText(Environment.NewLine);

            // Muestra tamaño y tipo
            rtxBox.SelectionFont = new Font("Segoe UI", 9, FontStyle.Italic);
            rtxBox.SelectionColor = Color.Gray;
            rtxBox.AppendText($"   {peso}, {tipo}{Environment.NewLine}");

            // Si es imagen, muestra miniatura
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                MostrarMiniaturaEnChat(filePath, alineadoDerecha);
            }

            // Hora
            rtxBox.SelectionFont = new Font("Segoe UI Emoji", 8, FontStyle.Regular);
            rtxBox.SelectionColor = Color.Gray;
            rtxBox.AppendText("   " + hora + Environment.NewLine + Environment.NewLine);

            rtxBox.ScrollToCaret();

            // Guarda la ruta para abrir al hacer clic
            if (enlacesArchivos == null)
                enlacesArchivos = new Dictionary<string, string>();
            enlacesArchivos[fi.Name] = filePath;
        }


        private void MostrarMiniaturaEnChat(string filePath, bool alineadoDerecha)
        {
            try
            {
                Image img = Image.FromFile(filePath);
                int thumbSize = 80;
                Image thumb = img.GetThumbnailImage(thumbSize, thumbSize, () => false, IntPtr.Zero);

                Clipboard.SetImage(thumb);
                rtxBox.SelectionStart = rtxBox.TextLength;
                rtxBox.Paste();
                rtxBox.AppendText(Environment.NewLine);
            }
            catch
            {
                // Si falla, solo ignora la miniatura
            }
        }


        private void rtxMensajeEnvia_TextChanged(object sender, EventArgs e)
        {
            btnEnviaMensaje.Enabled = !string.IsNullOrWhiteSpace(rtxMensajeEnvia.Text);
        }

        private void rtxBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.LinkText,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir el enlace: " + ex.Message);
            }
        }

        private void rtxMensajeEnvia_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                btnEnviaMensaje.PerformClick();
            }
        }

        private void RedondearTextBox(RichTextBox txt)
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(txt.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(txt.Width - radius, txt.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, txt.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            txt.Region = new Region(path);
        }

        private void RedondearBoton(Button btn)
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            btn.Region = new Region(path);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            ComunicaTXRX.InicioConstruirArchivo(); // Receiver prepares to receive file
            AgregarMensaje(nombreUsuarioLocal, "Esperando archivo entrante...", true);
        }

        // En Form1.cs

        private int InsertarMensajeEnviando(string fileName)
        {
            string spinner = "⏳"; // Puedes usar un gif si quieres, pero el emoji es simple y universal
            string tempMsg = $"{spinner} Enviando {Path.GetFileName(fileName)}...\n";
            int pos = rtxBox.TextLength;
            rtxBox.SelectionStart = pos;
            rtxBox.SelectionFont = new Font("Segoe UI", 10, FontStyle.Italic);
            rtxBox.SelectionColor = Color.Gray;
            rtxBox.AppendText(tempMsg);
            rtxBox.ScrollToCaret();
            return pos;
        }

        private void ReemplazarMensajePorArchivo(int pos, string fileName)
        {
            rtxBox.SelectionStart = pos;
            rtxBox.SelectionLength = rtxBox.Lines[pos / (rtxBox.TextLength / rtxBox.Lines.Length)].Length;
            rtxBox.SelectedText = ""; // Borra el mensaje temporal

            // Icono según tipo
            string ext = Path.GetExtension(fileName).ToLower();
            string icono = "📄";
            if (ext == ".pdf") icono = "📕";
            else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png") icono = "🖼️";
            else if (ext == ".doc" || ext == ".docx") icono = "📄";
            // Puedes agregar más tipos

            // Inserta el mensaje final con icono y nombre
            rtxBox.SelectionStart = rtxBox.TextLength;
            rtxBox.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            rtxBox.SelectionColor = Color.DarkCyan;
            rtxBox.AppendText($"{icono} {Path.GetFileName(fileName)}\n");
            rtxBox.ScrollToCaret();
        }

        private async void btnEnviarArchivo_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Todos los archivos (*.*)|*.*|Imágenes (*.jpg;*.png)|*.jpg;*.png|PDFs (*.pdf)|*.pdf";
                openFileDialog.Title = "Selecciona archivos para enviar";
                openFileDialog.Multiselect = true;


                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {

                        // Crea y muestra el panel de carga
                        var iconImage = Image.FromFile(Application.StartupPath + @"\img\archivo.png");
                        var spinnerImage = Image.FromFile(Application.StartupPath + @"\img\spinner.gif");
                        var panel = new FileSendingPanel(file, iconImage, spinnerImage)
                        {
                            Left = rtxBox.Left,
                            Top = rtxBox.Bottom + 10
                        };
                        this.Controls.Add(panel);
                        panel.BringToFront();

                        int pos = InsertarMensajeEnviando(file);

                        await Task.Run(() => ComunicaTXRX.IniciaEnvioArchivo(file));


                        // Quita el panel y muestra el mensaje final
                        this.Controls.Remove(panel);
                        AgregarMensaje(nombreUsuarioLocal, $"Archivo enviado: {Path.GetFileName(file)}", true);

                        // Al terminar, reemplaza el mensaje
                        this.Invoke((Action)(() =>
                        {
                            ReemplazarMensajePorArchivo(pos, file);
                        }));
                    }
                }
            }
        }


        private void panelSuperior_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panelSuperior_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void rtxBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void rtxBox_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
    public class FileSendingPanel : Panel
    {
        public PictureBox Icon { get; private set; }
        public Label FileNameLabel { get; private set; }
        public PictureBox Spinner { get; private set; }

        public FileSendingPanel(string fileName, Image iconImage, Image spinnerImage)
        {
            this.Width = 300;
            this.Height = 60;
            this.BackColor = Color.FromArgb(240, 240, 240);

            Icon = new PictureBox
            {
                Image = iconImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 40,
                Height = 40,
                Left = 10,
                Top = 10
            };
            this.Controls.Add(Icon);

            FileNameLabel = new Label
            {
                Text = Path.GetFileName(fileName),
                AutoSize = false,
                Width = 180,
                Height = 40,
                Left = 60,
                Top = 10,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            this.Controls.Add(FileNameLabel);

            Spinner = new PictureBox
            {
                Image = spinnerImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 32,
                Height = 32,
                Left = 250,
                Top = 14
            };
            this.Controls.Add(Spinner);
        }
    }

}