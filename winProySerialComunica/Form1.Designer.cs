using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace winProySerialComunica
{
    partial class FormChatApp
    {
        // The error CS1061 indicates that the `RichTextBox` class does not have a method called `InsertLink`.  
        // To fix this, you can implement a helper method to simulate the behavior of inserting a clickable link.  
        // Below is the updated code with a custom method to handle this functionality.  



        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormChatApp));
            this.btnEnviaMensaje = new System.Windows.Forms.Button();
            this.rtxBox = new System.Windows.Forms.RichTextBox();
            this.rtxMensajeEnvia = new System.Windows.Forms.RichTextBox();
            this.panelSuperior = new System.Windows.Forms.Panel();
            this.btnEnviarArchivo = new System.Windows.Forms.Button();
            this.btnMinimizar = new System.Windows.Forms.Button();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.panelSuperior.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnEnviaMensaje
            // 
            this.btnEnviaMensaje.BackColor = System.Drawing.Color.Transparent;
            this.btnEnviaMensaje.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnEnviaMensaje.Font = new System.Drawing.Font("Microsoft YaHei", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEnviaMensaje.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(72)))), ((int)(((byte)(74)))));
            this.btnEnviaMensaje.Location = new System.Drawing.Point(551, 610);
            this.btnEnviaMensaje.Name = "btnEnviaMensaje";
            this.btnEnviaMensaje.Size = new System.Drawing.Size(45, 45);
            this.btnEnviaMensaje.TabIndex = 1;
            this.btnEnviaMensaje.UseVisualStyleBackColor = false;
            this.btnEnviaMensaje.Click += new System.EventHandler(this.button2_Click);
            // 
            // rtxBox
            // 
            this.rtxBox.BackColor = System.Drawing.Color.White;
            this.rtxBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtxBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.rtxBox.ForeColor = System.Drawing.SystemColors.WindowText;
            this.rtxBox.HideSelection = false;
            this.rtxBox.Location = new System.Drawing.Point(48, 75);
            this.rtxBox.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.rtxBox.Name = "rtxBox";
            this.rtxBox.ReadOnly = true;
            this.rtxBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtxBox.Size = new System.Drawing.Size(548, 514);
            this.rtxBox.TabIndex = 4;
            this.rtxBox.Text = "";
            this.rtxBox.TextChanged += new System.EventHandler(this.rtxBox_TextChanged_1);
            // 
            // rtxMensajeEnvia
            // 
            this.rtxMensajeEnvia.BackColor = System.Drawing.Color.White;
            this.rtxMensajeEnvia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtxMensajeEnvia.Cursor = System.Windows.Forms.Cursors.Default;
            this.rtxMensajeEnvia.Location = new System.Drawing.Point(105, 610);
            this.rtxMensajeEnvia.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.rtxMensajeEnvia.Name = "rtxMensajeEnvia";
            this.rtxMensajeEnvia.Size = new System.Drawing.Size(436, 43);
            this.rtxMensajeEnvia.TabIndex = 5;
            this.rtxMensajeEnvia.Text = "";
            this.rtxMensajeEnvia.TextChanged += new System.EventHandler(this.rtxMensajeEnvia_TextChanged);
            // 
            // panelSuperior
            // 
            this.panelSuperior.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.panelSuperior.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelSuperior.BackgroundImage")));
            this.panelSuperior.Controls.Add(this.btnEnviarArchivo);
            this.panelSuperior.Controls.Add(this.btnMinimizar);
            this.panelSuperior.Controls.Add(this.btnCerrar);
            this.panelSuperior.Controls.Add(this.rtxBox);
            this.panelSuperior.Controls.Add(this.rtxMensajeEnvia);
            this.panelSuperior.Controls.Add(this.btnEnviaMensaje);
            this.panelSuperior.Cursor = System.Windows.Forms.Cursors.Default;
            this.panelSuperior.ForeColor = System.Drawing.Color.Cyan;
            this.panelSuperior.Location = new System.Drawing.Point(24, 12);
            this.panelSuperior.Name = "panelSuperior";
            this.panelSuperior.Size = new System.Drawing.Size(640, 668);
            this.panelSuperior.TabIndex = 6;
            this.panelSuperior.Paint += new System.Windows.Forms.PaintEventHandler(this.panelSuperior_Paint_1);
            // 
            // btnEnviarArchivo
            // 
            this.btnEnviarArchivo.BackColor = System.Drawing.Color.Transparent;
            this.btnEnviarArchivo.ForeColor = System.Drawing.Color.Transparent;
            this.btnEnviarArchivo.Location = new System.Drawing.Point(48, 610);
            this.btnEnviarArchivo.Name = "btnEnviarArchivo";
            this.btnEnviarArchivo.Size = new System.Drawing.Size(47, 45);
            this.btnEnviarArchivo.TabIndex = 8;
            this.btnEnviarArchivo.UseVisualStyleBackColor = false;
            this.btnEnviarArchivo.Click += new System.EventHandler(this.btnEnviarArchivo_Click_1);
            // 
            // btnMinimizar
            // 
            this.btnMinimizar.Location = new System.Drawing.Point(519, 13);
            this.btnMinimizar.Name = "btnMinimizar";
            this.btnMinimizar.Size = new System.Drawing.Size(36, 38);
            this.btnMinimizar.TabIndex = 7;
            this.btnMinimizar.UseVisualStyleBackColor = true;
            // 
            // btnCerrar
            // 
            this.btnCerrar.Location = new System.Drawing.Point(561, 13);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(36, 38);
            this.btnCerrar.TabIndex = 6;
            this.btnCerrar.UseVisualStyleBackColor = true;
            this.btnCerrar.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormChatApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 27F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(72)))), ((int)(((byte)(74)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(692, 692);
            this.Controls.Add(this.panelSuperior);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI Emoji", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.Name = "FormChatApp";
            this.Text = "ChatApp";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panelSuperior.ResumeLayout(false);
            this.ResumeLayout(false);

        }



        #endregion
        private System.Windows.Forms.Button btnEnviaMensaje;
        private System.Windows.Forms.RichTextBox rtxBox;
        private System.Windows.Forms.RichTextBox rtxMensajeEnvia;
        private System.Windows.Forms.Panel panelSuperior;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.Button btnMinimizar;
        private System.Windows.Forms.Button btnEnviarArchivo;
    }
}

