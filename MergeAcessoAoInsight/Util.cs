using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MergeAcessoAoInsight
{
    public static class Util
    {
        public static Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
        public static string padraoRemoverLink = @"<a( |\r\n|\n|\r)+(href|name)=(.|\r\n|\n)*?<\/a>";

        public static string EncontraLinksRemover(string html)
        {
            //string padrao1 = @"<a[ |\r\n]+href=\""[#a-zA-Z0-9 \.\/_]+\"">([a-zA-Z0-9À-ÿ ,\.\/=<>\-':]|\r\n)+<\/a>";
            //string padrao2 = @"<a href=\""[#a-zA-Z0-9 \.\/_]+\"">([a-zA-Z0-9À-ÿ ,\.\/=<>\-':]|\r\n)+<\/a>";
            //Remover(ref html, padrao1);
            RemoverLink(ref html, padraoRemoverLink);

            //remove o [Retorna]
            string padraoRetorna = @"\[[a-zA-Z0-9À-ÿ ,\.\/=<>\-':|\r\n|\n|\r]*Retorna(.|\r\n|\n|\r)*?]";
            RemoverPadrao(ref html, padraoRetorna);

            return html;
        }

        private static void RemoverLink(ref string html, string padrao)
        {
            int posicaoCorrigida = 0;
            Regex r = new Regex(padrao);
            Match m = r.Match(html);
            
            while (m.Success)
            {
                html = html.Remove(m.Index + posicaoCorrigida, m.Length); //remove todo o link
                var substituir = RemoverString(m.ToString(), "<a", ">");
                substituir = substituir.Replace("</a>", "");
                html = html.Insert(m.Index + posicaoCorrigida, substituir); //substituiu colocando tudo que foi removido menos o link

                posicaoCorrigida += substituir.Length - m.Length;

                //########################################################
                // está travando aqui quando vai pegar o próximo match
                // Colocar condition: m.Index>35889
                //########################################################
                
                //var task = Task.Run(() => {
                //    m = m.NextMatch(); //vai para o proximo
                //});
                //if (!task.Wait(TimeSpan.FromSeconds(5)))
                //    break;

                m = m.NextMatch(); //vai para o proximo
            }
        }

        private static void RemoverPadrao(ref string html, string padrao)
        {
            int posicaoCorrigida = 0;
            Regex r = new Regex(padrao);
            Match m = r.Match(html);

            while (m.Success)
            {
                //Console.WriteLine("Match: " + m.ToString());

                html = html.Remove(m.Index + posicaoCorrigida, m.Length); //remove todo

                posicaoCorrigida -= m.Length;
                m = m.NextMatch(); //vai para o proximo
            }
        }

        public static string RemoverString(string textoOriginal, string inicio, string fim)
        {
            try
            {
                int posInicial = 0;
                int posFim = 0;
                while (posInicial != -1 && posFim != -1)
                {
                    posInicial = textoOriginal.IndexOf(inicio, posFim);
                    posFim = posInicial != -1 ? textoOriginal.IndexOf(fim, posInicial) : -1;
                    int tam = (posFim + fim.Length) - posInicial;

                    if (posInicial != -1 && posFim != -1)
                    {
                        textoOriginal = textoOriginal.Remove(posInicial, tam);
                        posFim -= tam;
                    }
                }

                return textoOriginal;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private static void GuardarLinkImportante(ref string html)
        {
            var padrao = @"<a href=\""..\/arquivo_textos_theravada\/[a-zA-Z0-9]+\.php\"">.*<\/a>";
            Regex r = new Regex(padrao);
            Match m = r.Match(html);

            while (m.Success)
            {
                //Console.WriteLine("Match: " + m.ToString());

                html = html.Remove(m.Index, m.Length); //remove todo o link
                var substituir = m.ToString().Replace("<a ", "<x ");
                html = html.Insert(m.Index, substituir); //substituiu

                m = r.Match(html);
                m = m.NextMatch(); //vai para o proximo
            }
        }

        private static void RecuperarLinkImportante(ref string html)
        {
            var padrao = @"<x href=\""..\/arquivo_textos_theravada\/[a-zA-Z0-9]+\.php\"">.*<\/a>";
            Regex r = new Regex(padrao);
            Match m = r.Match(html);

            while (m.Success)
            {
                html = html.Remove(m.Index, m.Length); //remove todo o link
                var substituir = m.ToString().Replace("<x ", "<a ");
                html = html.Insert(m.Index, substituir); //substituiu

                m = r.Match(html);
                m = m.NextMatch(); //vai para o proximo
            }
        }

        public static void TrocarLinkImportantePorTexto(ref string html, string path)
        {
            int posicaoAdicao = 0;
            var padrao = @"<p .*<a href=\""..\/arquivo_textos_theravada\/[a-zA-Z0-9]+\.php\"">.*<\/a>.*<\/p>";
            Regex r = new Regex(padrao);
            Match m = r.Match(html);

            while (m.Success)
            {
                var textoDoLink = PegarTextoLink(m.ToString(), path);
                textoDoLink = Util.RemoverString(textoDoLink, "<hr", ">");
                RemoverLink(ref textoDoLink, padraoRemoverLink);
                //RemoverAvisoDistribuicao(ref textoDoLink);
                //textoDoLink = Util.RemoverString(textoDoLink, "<span lang=PT-BR style='font-size:" + Environment.NewLine +
                //    "7.0pt;mso-ansi-language:PT-BR'><b> Somente para distribuição gratuita.</b><br>",
                //    "De outra forma todos os direitos estão reservados. </i> <o:p></o:p></span></p>");

                var pontilhado = "<p>............................................................................................................................</p><br>";
                textoDoLink = pontilhado + textoDoLink + pontilhado;
                html = html.Insert(m.Index + m.Length + posicaoAdicao, textoDoLink); //substituiu
                
                m = m.NextMatch(); //vai para o proximo
                posicaoAdicao += textoDoLink.Length;
            }
        }

        public static string LimparArquivo(string arquivoDestino, bool salvarConteudo = true)
        {
            string conteudoArqFinal = File.ReadAllText(arquivoDestino, Util.encoding);

            conteudoArqFinal = Util.RemoverString(conteudoArqFinal, "<!", ">");
            conteudoArqFinal = Util.RemoverString(conteudoArqFinal, "<img", ">");
            conteudoArqFinal = Util.RemoverString(conteudoArqFinal, "<b> >> Próximo Sutta", "</p>");

            if (salvarConteudo)
                File.WriteAllText(arquivoDestino, conteudoArqFinal, Util.encoding);

            return conteudoArqFinal;
        }

        public static string ExtrairSuttaConteudo(string conteudo)
        {
            var separadores = new string[2];
            separadores[0] = "<!-- INICIO DO TEXTO -->";
            separadores[1] = "<!-- FIM DO TEXTO -->";
            var result = conteudo.Split(separadores, StringSplitOptions.None);

            if (result.Count() != 3)
                throw new Exception($"Conteúdo fora do padrao. Não foi possível extrair o conteudo.");

            return result[1];
        }

        public static string ExtrairSuttaArquivo(string arquivo)
        {
            string textoTodo = File.ReadAllText(arquivo, Util.encoding);

            return ExtrairSuttaConteudo(textoTodo);
        }

        public static void RemoverAvisoDistribuicao(ref string html)
        {
            var padrao = @"(\<b\>){0,1}( )*Somente para distribuição gratuita\.[a-zA-ZÀ-ÿ \/\-\.\<\>|\r\n]+De outra forma todos os direitos estão reservados\.( )*(\<\/i\>){0,1}";
            Regex r = new Regex(padrao);
            Match m = r.Match(html);

            if (m.Success)
                html = html.Remove(m.Index, m.Length);
        }


        private static string PegarTextoLink(string linha, string path)
        {
            string conteudo = "";
            var padrao = @"href=\""[a-zA-Z0-9 \.\/_]+\""";
            Regex r = new Regex(padrao);
            Match m = r.Match(linha);

            if (m.Success){
                var caminho = path + m.ToString().Replace("href=\"","").Replace("\"","");
                conteudo = LimparArquivo(caminho, false);
                RemoverAvisoDistribuicao(ref conteudo);
                conteudo = Util.ExtrairSuttaConteudo(conteudo);
            }

            return conteudo;
        }
    }
}