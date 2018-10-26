using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MergeAcessoAoInsight
{
    public partial class Form1 : Form
    {
        private static string path = @"E:\AcessoAoInsight\merge\";
        private static string arquivoDestino = path + "arquivo-unico.html";
        private static string arquivoHR = path + "arquivo-unico2.html";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<string> listaDeArquivos;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                listaDeArquivos = GetPhpsEspecificadosArqTxt();
                if (listaDeArquivos.Count == 0)
                {
                    listaDeArquivos = GetArquivos();
                    listaDeArquivos = OrdenarArquivos(listaDeArquivos);
                }

                MergeArquivos(listaDeArquivos);
                Util.LimparArquivo(arquivoDestino);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            MessageBox.Show("Processamento concluído");
        }

        private List<string> GetPhpsEspecificadosArqTxt()
        {
            var arquivos = File.ReadAllLines(path + "phps.txt").ToList();
            var arquivosAux = new List<string>();
            foreach(var arq in arquivos)
                arquivosAux.Add(@"E:\AcessoAoInsight\sutta\" + arq);
            return arquivosAux;
        }

        private List<string> OrdenarArquivos(List<string> listaDeArquivos)
        {
            string sigla = textBoxSigla.Text;
            string padrao = $"{sigla}\\.[0-9]+.php";
            Regex r = new Regex(padrao, RegexOptions.Singleline);
            var listaIntermediaria = new List<ArqOrnado>();

            foreach(var arq in listaDeArquivos)
            {
                string trecho = r.Match(arq).ToString();

                if (trecho == "")
                    throw new Exception("Não foi encontrado o numero do sutta");

                string numStr = trecho.Replace(sigla+".", "").Replace(".php", "");

                listaIntermediaria.Add(new ArqOrnado { num = int.Parse(numStr), nome = arq });
            }

            return listaIntermediaria.OrderBy(l => l.num).Select(l => l.nome).ToList();
        }

        class ArqOrnado {
            public int num { get; set; }
            public string nome { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            string conteudoArqFinal = File.ReadAllText(arquivoDestino, Util.encoding);

            conteudoArqFinal = AjustarTagHr(conteudoArqFinal);
            Util.TrocarLinkImportantePorTexto(ref conteudoArqFinal, path);
            File.WriteAllText(arquivoHR, conteudoArqFinal, Util.encoding);
            Cursor.Current = Cursors.Default;
            MessageBox.Show("Processamento concluído");
        }

        private string AjustarTagHr(string conteudo)
        {
            string hrIncluir = "<hr size=2 width=\"100%\"><p class=Normal>&nbsp;</p>";
            string tagTitulo = "<p class=Tit3 align=center style='text-align:center'>";
            string tagParagrafo1 = "<p class=Normal><span lang=PT-BR style='mso-ansi-language:PT-BR'>";
            string hrProcurar = "<hr size=2 width=\"100%\" align=center>";

            int posTitulo1 = 0;
            int posParagrafo1 = 0;
            int posHr = 0;
            while (posParagrafo1 < conteudo.Length && posTitulo1 != -1 && posParagrafo1 != -1)
            {
                posTitulo1 = conteudo.IndexOf(tagTitulo, posParagrafo1);
                posParagrafo1 = conteudo.IndexOf(tagParagrafo1, posTitulo1 + tagTitulo.Length);

                if (posTitulo1 != -1)
                    posHr = conteudo.IndexOf(hrProcurar, posTitulo1);

                if (posHr != -1 && posParagrafo1 != -1 && posHr < posParagrafo1)
                    conteudo = conteudo.Remove(posHr, hrProcurar.Length);

                if (posTitulo1 != -1)
                    conteudo = conteudo.Insert(posTitulo1 - 1, hrIncluir);
            }

            return conteudo;
        }

        private void MergeArquivos(List<string> listaDeArquivos)
        {
            File.WriteAllText(arquivoDestino, "", Util.encoding);
            string conteudoInicio = File.ReadAllText(path + "topo.inc", Util.encoding);
            File.AppendAllText(arquivoDestino, conteudoInicio, Util.encoding);

            foreach (var arq in listaDeArquivos)
            {
                var conteudoMeio = Util.ExtrairSuttaArquivo(arq);
                Util.RemoverAvisoDistribuicao(ref conteudoMeio);
                conteudoMeio = Util.EncontraLinksRemover(conteudoMeio);
                File.AppendAllText(arquivoDestino, conteudoMeio, Util.encoding);
            }

            string conteudoFim = File.ReadAllText(path + "base.inc", Util.encoding);
            File.AppendAllText(arquivoDestino, conteudoFim, Util.encoding);
        }

        private List<string> GetArquivos(string padrao = "*.php")
        {
            if (Directory.Exists(path))
            {
                // This path is a directory
                return ProcessDirectory(path, padrao);
            }
            else
            {
                throw new Exception($"Diretório [{path}] não existe!");
            }
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public List<string> ProcessDirectory(string targetDirectory, string padrao)
        {
            var listaDeArquivos = new List<string>();

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory, padrao);
            foreach (string fileName in fileEntries)
                listaDeArquivos.Add(fileName);
            
            return listaDeArquivos;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            VerificarLinkImportante();
        }

        private void VerificarLinkImportante()
        {
            path = @"E:\AcessoAoInsight\sutta\";
            List<string> listaDeArquivos = GetArquivos($"{textBoxSigla.Text}*.php");

            foreach (var arq in listaDeArquivos)
            {
                var html = File.ReadAllText(arq, Util.encoding);
                var padrao = @"<a href=\""..\/arquivo_textos_theravada\/[a-zA-Z0-9]+\.php\"">.*<\/a>";
                Regex r = new Regex(padrao);
                Match m = r.Match(html);

                while (m.Success)
                {
                    Console.WriteLine($"Encontrou no arquivo {arq} - posicao: {m.Index} - match: {m.ToString()}");
                    m = m.NextMatch(); //vai para o proximo
                }
            }
        }
    }
}