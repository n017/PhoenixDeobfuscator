using System;
using System.IO;
using System.Windows.Forms;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace PhoenixDeobfuscator
{
    public partial class Form1 : Form
    {
        #region Declarations

        public string DirectoryName = "";
        public int ConstantKey;
        public int ConstantNum;
        public MethodDef Methoddecryption;
        public TypeDef Typedecryption;
        public MethodDef MethodeResource;
        public TypeDef TypeResource;
        public ModuleDefMD module;
        public int x;
        public int DeobedStringNumber;

        #endregion

        #region Designer

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            label2.Text = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Browse for target assembly";
            openFileDialog.InitialDirectory = "c:\\";
            if (DirectoryName != "")
            {
                openFileDialog.InitialDirectory = this.DirectoryName;
            }
            openFileDialog.Filter = "All files (*.exe,*.dll)|*.exe;*.dll";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                textBox1.Text = fileName;
                int num = fileName.LastIndexOf("\\", StringComparison.Ordinal);
                if (num != -1)
                {
                    DirectoryName = fileName.Remove(num, fileName.Length - num);
                }
                if (DirectoryName.Length == 2)
                {
                    DirectoryName += "\\";
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            module = ModuleDefMD.Load(textBox1.Text);
            FindStringDecrypterMethods(module);
            DecryptStringsInMethod(module, Methoddecryption);
            string text2 = Path.GetDirectoryName(textBox1.Text);
            if (!text2.EndsWith("\\"))
            {
                text2 += "\\";
            }
            string path = text2 + Path.GetFileNameWithoutExtension(textBox1.Text) + "_patched" +
                          Path.GetExtension(textBox1.Text);
            var opts = new ModuleWriterOptions(module);
            opts.Logger = DummyLogger.NoThrowInstance;
            module.Write(path, opts);
            label2.Text = "Successfully decrypted " + DeobedStringNumber + " strings !";
        }

        private void TextBox1DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TextBox1DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array array = (Array) e.Data.GetData(DataFormats.FileDrop);
                if (array != null)
                {
                    string text = array.GetValue(0).ToString();
                    int num = text.LastIndexOf(".", StringComparison.Ordinal);
                    if (num != -1)
                    {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".exe" || text2 == ".dll")
                        {
                            Activate();
                            textBox1.Text = text;
                            int num2 = text.LastIndexOf("\\", StringComparison.Ordinal);
                            if (num2 != -1)
                            {
                                DirectoryName = text.Remove(num2, text.Length - num2);
                            }
                            if (DirectoryName.Length == 2)
                            {
                                DirectoryName += "\\";
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Method

         private void FindStringDecrypterMethods(ModuleDefMD module)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody == false)
                        continue;
                    if (method.Body.HasInstructions)
                    {
                        var instrs = method.Body.Instructions;
                        if (instrs.Count > 45)
                        {
                            for (int i = 0; i < instrs.Count - 3; i++)
                            {
                                if (instrs[i].OpCode.Code == Code.Ldarg_0 && instrs[1].OpCode.Code == Code.Callvirt &&
                                    instrs[2].OpCode.Code == Code.Stloc_0 && instrs[3].OpCode.Code == Code.Ldloc_0 &&
                                    instrs[24].OpCode.Code == Code.Xor && instrs[31].OpCode.Code == Code.Shl)
                                {
                                    Methoddecryption = method;
                                    Typedecryption = type;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DecryptStringsInMethod(ModuleDefMD module, MethodDef Methoddecryption)
        {
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody)
                        break;
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            if (method.Body.Instructions[i + 1].OpCode == OpCodes.Call &&
                                method.Body.Instructions[i + 1].Operand.ToString().Contains(Methoddecryption.ToString()))
                            {
                                CilBody body = method.Body;
                                var string2decrypt = method.Body.Instructions[i].Operand.ToString();
                                string decryptedstring = DecryptString(string2decrypt);

                                body.Instructions[i].OpCode = OpCodes.Ldstr;
                                body.Instructions[i].Operand = decryptedstring;
                                body.Instructions.RemoveAt(i + 1);
                                DeobedStringNumber = DeobedStringNumber + 1;
                            }
                        }
                    }
                }
            }
        }

        public static string DecryptString(string str)
        {
            //Decryption method is constant, either you use cflow or not...
            int length = str.Length;
            char[] array = new char[length];
            for (int i = 0; i < array.Length; i++)
            {
                char c = str[i];
                byte b = (byte) (c ^ length - i);
                byte b2 = (byte) ((c >> 8) ^ i);
                array[i] = (char) (b2 << 8 | b);
            }
            return string.Intern(new string(array));
        }

    }

    #endregion


}
