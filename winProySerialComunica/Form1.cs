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

        public FormChatApp()
        {
            InitializeComponent();
            rtxBox.LinkClicked += rtxBox_LinkClicked;

            ComunicaTXRX = new classComunicacion("COM1", 115200); // Changed from 115200 to match classComunicacion
            ComunicaTXRX.LlegoMensaje += ComunicaTXRX_LLegomensaje;
            ComunicaTXRX.LlegoArchivo += ComunicaTXRX_LlegoArchivo;
            ComunicaTXRX.ProgresoEnvio += ComunicaTXRX_ProgresoEnvio; // NUEVA LÍNEA
            ComunicaTXRX.ProgresoRecepcion += ComunicaTXRX_ProgresoRecepcion; // NUEVA LÍNEA

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

        private void ComunicaTXRX_ProgresoEnvio(string fileName, long enviado, long total)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    ComunicaTXRX_ProgresoEnvio(fileName, enviado, total);
                }));
            }
            else
            {
                double porcentaje = (double)enviado * 100 / total;

                // CORRECCIÓN: Mostrar progreso más detallado y solo en intervalos específicos
                if (porcentaje == 0 || porcentaje >= 100 || (int)porcentaje % 20 == 0)
                {
                    string mensaje = $"Enviando {fileName}: {porcentaje:F1}% ({enviado:N0}/{total:N0} bytes)";

                    // En lugar de MessageBox, actualizar el chat
                    AgregarMensaje("Sistema", mensaje, false);

                    // Si quieres mantener el MessageBox para debug, úsalo solo al 100%
                    if (porcentaje >= 100)
                    {
                        MessageBox.Show($"Archivo {fileName} enviado completamente");
                    }
                }
            }
        }



        private void ComunicaTXRX_ProgresoRecepcion(string fileName, long recibido, long total)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    ComunicaTXRX_ProgresoRecepcion(fileName, recibido, total);
                }));
            }
            else
            {
                double porcentaje = (double)recibido * 100 / total;

                if (porcentaje == 0 || porcentaje >= 100 || (int)porcentaje % 20 == 0)
                {
                    string mensaje = $"Recibiendo {fileName}: {porcentaje:F1}% ({recibido:N0}/{total:N0} bytes)";
                    AgregarMensaje("Sistema", mensaje, false);
                }
            }
        }
        // Rest of the code remains unchanged
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
                    AgregarMensaje(nombreUsuarioRemoto, $"Archivo recibido: {Path.GetFileName(fileName)} ({fileName})", false);
                }));
            }
            else
            {
                AgregarMensaje(nombreUsuarioRemoto, $"Archivo recibido: {Path.GetFileName(fileName)} ({fileName})", false);
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

        private void btnEnviarArchivo_Click_1(object sender, EventArgs e)
        {


            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*|Images (*.jpg;*.png)|*.jpg;*.png|PDFs (*.pdf)|*.pdf";
                openFileDialog.Title = "Selecciona archivos para enviar";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        // Verificar que el archivo existe y no está vacío
                        if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                        {
                            ComunicaTXRX.IniciaEnvioArchivo(filePath);
                            AgregarMensaje(nombreUsuarioLocal, $"Enviando archivo: {Path.GetFileName(filePath)} ({new FileInfo(filePath).Length} bytes)", true);
                        }
                        else
                        {
                            MessageBox.Show($"El archivo {Path.GetFileName(filePath)} no existe o está vacío.");
                        }
                    }
                }
            }

        }

        private void panelSuperior_Paint(object sender, PaintEventArgs e)
        {
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ComunicaTXRX?.Cerrar();
            base.OnFormClosed(e);
        }
    }
}
