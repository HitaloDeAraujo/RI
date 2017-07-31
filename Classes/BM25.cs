using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCC
{
    class BM25
    {
        static double k1 = 1, b = 0.75, avg_doclen;
        static string[] BMMatriz;
        static int[] FrequenciaIndividual;
        static int N;
        private static bool ModuloFuncionando;   //Indica que o modulo esta funcionando

        private static List<string> Vocabulario = new List<string>();
        private static DirectoryInfo Diretorio_Documentos;    //Informacoes do diretorio
        private static FileInfo[] Arquivos_Info;  //Informacoes dos arquivos
        private static string[] Arquivo;

        private static List<string> ListaLogs = new List<string>();

        #region BM25
        private static List<BitArray> listaBinariaBM25;
        private static string Porcentagem = "Aguardando Processo";

        /// <summary>
        /// Novo objeto BM25
        /// </summary>
        public BM25()
        {
            ModuloFuncionando = true;

            try
            {
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos

                N = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories).Count();

                if (Vocabulario.Count() == 0)
                    Vocabulario.AddRange(File.ReadAllLines(Configuracoes.Caminho_Vocabulario()));

                //if (BMMatriz == null)
                //    CarregarVetores();

                if (FrequenciaIndividual == null)
                {
                    string[] aux = File.ReadAllLines(Configuracoes.Diretorio_MP() + "Tamanho dos documentos.txt");

                    FrequenciaIndividual = new int[aux.Count()];

                    for (int i = 0; i < aux.Count(); i++)
                        FrequenciaIndividual[i] = int.Parse(aux[i]);
                }

                if (avg_doclen == 0)
                    avg_doclen = double.Parse(File.ReadAllText(Configuracoes.Diretorio_MP() + "Tamanho medio dos documentos.txt"));


                int cont = 0;
                //Adiciona cada arquivo do diretorio no vetor, ordenados pelo nome
                if (Arquivo == null)
                {
                    Arquivo = new string[Arquivos_Info.Count()];

                    foreach (FileInfo item in Arquivos_Info.OrderBy(p => p.Name))
                    {
                        Porcentagem = (cont * 100 / Arquivos_Info.Count()).ToString() + "%";

                        Arquivo[cont++] = item.FullName;
                    }

                    Porcentagem = "100%";
                }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro na inicialização do modelo BM25", ex.ToString()));

                ModuloFuncionando = false;
            }
        }

        /// <summary>
        /// Logs de processamento
        /// </summary>
        public static List<string> Logs()
        {
            return ListaLogs;
        }

        /// <summary>
        /// Porcentagem de conclusao
        /// </summary>
        public static string PorcentagemDeConclusao()
        {
            return Porcentagem;
        }

        /// <summary>
        /// Verifica se o modulo esta funcionando
        /// </summary>
        public static bool ModuloEmFuncionamento()
        {
            //Se a matriz nao estiver vazia e se o modulo nao encontrou erros
            if (/*Matriz != null &&*/ ModuloFuncionando && Arquivo != null && listaBinariaBM25.Count() != 0)
                return true;    //Funcionando
            else
                return false;   //Nao esta funcionando
        }

        /// <summary>
        /// Refaz as operacoes do modelo
        /// </summary>
        public static void RefazerBM25()
        {
            //CalcularTamanhoDocumentos();

            ConstruirMatrizBM();
        }

        /// <summary>
        /// Equacao Bij
        /// </summary>
        private double Bij(int fij, int len)
        {
            try
            {             
                return ((k1 + 1) * fij) / (k1 *  Math.Abs(((1 - b) + b * (len / avg_doclen))) + fij);
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao calcular Bij", ex.ToString()));

                ModuloFuncionando = false;
            }

            return 0;
        }

        /// <summary>
        /// Converte de binario para numero inteiro
        /// </summary>
        private static int BinParaInt(bool[] SeqBinaria)
        {
            double x = 0;

            try
            {
                for (int i = 0; i < 4; i++) //Percorre os bits e transforma para numero inteiro
                    if (SeqBinaria[i])
                        x += Math.Pow(2, 3 - i);
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao fazer conversão", ex.ToString()));

                ModuloFuncionando = false;
            }

            return (int)x;
        }

        string TermoDoVocabulario;

        //Verifica possibilidade de casamento de texto aproximado
        private bool VocabularioContem(string TermoUsuario)
        {
            foreach (string TermoVocabulario in Vocabulario)
                if (Sellers.Comparar(TermoUsuario, TermoVocabulario))
                {
                    TermoDoVocabulario = TermoVocabulario;
                    return true;
                }

            return false;
        }

        //Metrica de similaridade do BM25
        public SortedDictionary<string, double> Similaridade_BM25(string Frase)
        {
            SortedDictionary<string, double> DocumentoseRelevantes = new SortedDictionary<string, double>();    //Colecao de arquivos com nome e relevancia

            try
            {
                Frase = TratamentoDeTexto.Eliminacao_Stopwords(Frase);

                foreach (string Termo in Frase.Split(' '))
                    if (Vocabulario.Contains(Termo))    //Se o vocabulario contem o termo
                    {

                        int Indice = Vocabulario.IndexOf(Termo);    //Indice do termo no vocabulario

                        int ni = 0;  //Quantidade de documentos que contem o termo

                        bool[] aux = new bool[4];

                        for (int i = 0; i < listaBinariaBM25[Indice].Length; i += 4) //Percorre Matriz
                        {
                            aux[0] = listaBinariaBM25[Indice][i + 0];
                            aux[1] = listaBinariaBM25[Indice][i + 1];
                            aux[2] = listaBinariaBM25[Indice][i + 2];
                            aux[3] = listaBinariaBM25[Indice][i + 3];

                            if (BinParaInt(aux) != 0)   //Se contem o termo no documento
                                ni++;    //Incrementa o numero de documentos que contem o termo
                        }


                        //Calcula peso com base na formaula do modelo probabilistico
                        double Peso = Math.Log10((N - ni + 0.5) / (ni + 0.5));

                        for (int i = 0; i < listaBinariaBM25[Indice].Length; i += 4) //Para cada elemento de matriz
                        {
                            aux[0] = listaBinariaBM25[Indice][i + 0];
                            aux[1] = listaBinariaBM25[Indice][i + 1];
                            aux[2] = listaBinariaBM25[Indice][i + 2];
                            aux[3] = listaBinariaBM25[Indice][i + 3];

                            if (BinParaInt(aux) != 0)   //Se o termo existe no arquivo
                            {
                                if (DocumentoseRelevantes.ContainsKey(Arquivo[i / 4]))  //Se o documento ja estiver na colecao
                                    DocumentoseRelevantes[Arquivo[i / 4]] = double.Parse(DocumentoseRelevantes[Arquivo[i / 4]].ToString()) + Bij(BinParaInt(aux), FrequenciaIndividual[i / 4]) * Peso;  //Incrementa o peso
                                else
                                    DocumentoseRelevantes.Add(Arquivo[i / 4], Bij(BinParaInt(aux), FrequenciaIndividual[i / 4]) * Peso);    //Armazena o peso
                            }
                        }
                    }
                    else if (VocabularioContem(Termo))
                    {
                        int Indice = Vocabulario.IndexOf(TermoDoVocabulario);

                        int ni = 0;

                        bool[] aux = new bool[4];

                        for (int i = 0; i < listaBinariaBM25[Indice].Length; i += 4) //Percorre Matriz
                        {
                            aux[0] = listaBinariaBM25[Indice][i + 0];
                            aux[1] = listaBinariaBM25[Indice][i + 1];
                            aux[2] = listaBinariaBM25[Indice][i + 2];
                            aux[3] = listaBinariaBM25[Indice][i + 3];

                            if (BinParaInt(aux) != 0)   //Se contem o termo no documento
                                ni++;    //Incrementa o numero de documentos que contem o termo
                        }

                        //Calcula peso com base na formaula do modelo probabilistico
                        double Peso = Math.Log10((N - ni + 0.5) / (ni + 0.5));

                        for (int i = 0; i < listaBinariaBM25[Indice].Length; i += 4) //Para cada elemento de matriz
                        {
                            aux[0] = listaBinariaBM25[Indice][i + 0];
                            aux[1] = listaBinariaBM25[Indice][i + 1];
                            aux[2] = listaBinariaBM25[Indice][i + 2];
                            aux[3] = listaBinariaBM25[Indice][i + 3];

                            if (BinParaInt(aux) != 0)   //Se o termo existe no arquivo
                            {
                                if (DocumentoseRelevantes.ContainsKey(Arquivo[i / 4]))  //Se o documento ja estiver na colecao
                                    DocumentoseRelevantes[Arquivo[i / 4]] = double.Parse(DocumentoseRelevantes[Arquivo[i / 4]].ToString()) + Bij(BinParaInt(aux), FrequenciaIndividual[i / 4]) * Peso;  //Incrementa o peso
                                else
                                    DocumentoseRelevantes.Add(Arquivo[i / 4], Bij(BinParaInt(aux), FrequenciaIndividual[i / 4]) * Peso);    //Armazena o peso
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao calcular similaridade no BM25", ex.ToString()));
                ModuloFuncionando = false;
            }

            return DocumentoseRelevantes;
        }

        //Carregar vetores
        public static void CarregarVetores_BM25()
        {
            try
            {
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos

                ListaLogs.Add("|Carregando vetores BM25                                   |");
                listaBinariaBM25 = new List<BitArray>();

                N = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories).Count();

                bool[] vetordebits = new bool[4];

                StreamReader sr = new StreamReader(Configuracoes.Caminho_VetoresBM25());

                while (!sr.EndOfStream)
                {
                    string source = sr.ReadLine();

                    BitArray x = new BitArray(N * 4);
                    int contador = 0;
                    for (int i = 0; i < source.Length; i++)
                    {
                        switch (source[i])
                        {
                            case '0': vetordebits = new bool[] { false, false, false, false }; break;
                            case '1': vetordebits = new bool[] { false, false, false, true }; break;
                            case '2': vetordebits = new bool[] { false, false, true, false }; break;
                            case '3': vetordebits = new bool[] { false, false, true, true }; break;
                            case '4': vetordebits = new bool[] { false, true, false, false }; break;
                            case '5': vetordebits = new bool[] { false, true, false, true }; break;
                            case '6': vetordebits = new bool[] { false, true, true, false }; break;
                            case '7': vetordebits = new bool[] { false, true, true, true }; break;
                            case '8': vetordebits = new bool[] { true, false, false, false }; break;
                            case '9': vetordebits = new bool[] { true, false, false, true }; break;
                            default: vetordebits = new bool[] { true, false, false, true };
                                break;
                        }

                        x[contador++] = vetordebits[0];
                        x[contador++] = vetordebits[1];
                        x[contador++] = vetordebits[2];
                        x[contador++] = vetordebits[3];
                    }

                    listaBinariaBM25.Add(x);
                }

                sr.Close();

                BMMatriz = null;
                ListaLogs.Add("|Vetores BM25 carregados - - - - - OK                          |");
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao calcular vetores do BM25", ex.ToString()));

                ModuloFuncionando = false;
            }

            return;
        }
        #endregion

        #region Construcao_BaseBM25
        //static System.Linq.IOrderedEnumerable<System.IO.FileInfo> x = Arquivos_Info.OrderBy(p => p.Name);

        static string[][] VetorDocumentosBM25;

        /// <summary>
        /// Frequencia do termo no documento
        /// </summary>
        private static int Frequencia(int IndiceVoc, int IndiceDoc)
        {
            int cont = 0;

            try
            {
                if (VetorDocumentosBM25 == null)
                {
                    //Vetor de documentos e linhas destes
                    VetorDocumentosBM25 = new string[Arquivos_Info.Count()][];

                    string[] splitAux = { " ", "<", ">", "/key", "/think" };

                    ListaLogs.Add("Carregando arquivos");
                    foreach (FileInfo Arquivo in Arquivos_Info.OrderBy(p => p.Name))    //Para cada arquivo
                    {
                        Porcentagem = (cont * 100 / Arquivos_Info.Count()).ToString();
                        string[] aux = TratamentoDeTexto.TransformacaoLexica(File.ReadAllText(Arquivo.FullName)).Split(splitAux, StringSplitOptions.RemoveEmptyEntries);
                        VetorDocumentosBM25[cont++] = aux;  //Carrega todas as linhas
                    }
                }

                string Termo = Vocabulario[IndiceVoc];

                cont = 0;

                foreach (string item in VetorDocumentosBM25[IndiceDoc])
                    if (item == Termo)
                        cont++;
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao calcular frequencia do termo", ex.ToString()));

                ModuloFuncionando = false;
            }

            if (cont > 9)
                return 9;
            else
                return cont;
        }

        /// <summary>
        /// Calcula tamanho dos documentos
        /// </summary>
        private static void CalcularTamanhoDocumentos()
        {
            try
            {
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos

                string doc;
                int QuantidadeDeArquivos = Arquivos_Info.Count();
                int tamanhoindividual, tamanhototal = 0;
                string[] aux = new string[QuantidadeDeArquivos];

                int cont = 0;

                //Para cada documento
                foreach (FileInfo item in Arquivos_Info.OrderBy(p => p.Name))
                {
                    Porcentagem = (cont * 100 / QuantidadeDeArquivos).ToString() + "%";

                    doc = File.ReadAllText(item.FullName);  //Le texto do documento

                    tamanhoindividual = doc.Split(' ').Count();

                    aux[cont] = tamanhoindividual.ToString();

                    tamanhototal += tamanhoindividual;

                    cont++;
                }

                Porcentagem = "100%";

                avg_doclen = (float)(tamanhototal / QuantidadeDeArquivos);  //Tamanho medio dos documentos

                //Escreve as informacoes obtidas
                File.WriteAllLines(Configuracoes.Diretorio_MP() + "Tamanho dos documentos.txt", aux);
                File.WriteAllText(Configuracoes.Diretorio_MP() + "Tamanho medio dos documentos.txt", avg_doclen.ToString());
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao calcular tamanho de documento", ex.ToString()));

                ModuloFuncionando = false;
            }

            return;
        }

        private static readonly object ReadOnlyObject4 = new object();   //Objeto para bloqueio de recursos

        static int IndiceBM25 = 0;
        private static void Threads_ConstruirMatrizBM()
        {
            int i;

            lock (ReadOnlyObject4)   //Bloqueia acesso a pilha Documentos
            {
                i = IndiceBM25;
                IndiceBM25++;
            }

            try
            {
                string aux = "";

                for (int j = 0; j < Arquivos_Info.Count(); j++)
                {
                    //if (Matriz[i][j] == '1')
                    aux += Frequencia(i, j);
                    //else
                    //    aux += 0;
                }

                //BMMatriz[i] = aux;

                BMMatriz[i] = aux;
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao construir matriz BM25", ex.ToString()));

                ModuloFuncionando = false;
            }

            //try
            //{
            //    //Escreve todas as linhas
            //    //File.WriteAllText(Configuracoes.Diretorio_ColecaoBM25() + "\\" + i + ".txt", aux, Encoding.UTF8);
            //}
            //catch (Exception)
            //{
            //    ListaLogs.Add();
            //}

            return;
        }

        private static void ConstruirMatrizBM()
        {
            try
            {
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos

                Vocabulario.AddRange(File.ReadAllLines(Configuracoes.Caminho_Vocabulario()));
                int tamanhovocabulario = Vocabulario.Count;

                BMMatriz = new string[tamanhovocabulario];

                int Contador = 0;
                int Aux = 0;
                Contador = 0;
                int NumMaximoThreads = 10;


                DirectoryInfo D = new DirectoryInfo(Configuracoes.Diretorio_ColecaoBM25());
                FileInfo[] AD = D.GetFiles("*.txt", SearchOption.AllDirectories);

                IndiceBM25 = AD.Count();
                //IndiceBM25 = 2200; tamanhovocabulario = 2201;
                Contador = IndiceBM25;


                if (IndiceBM25 != tamanhovocabulario)
                {
                    Thread[] NovaThread = new Thread[tamanhovocabulario];

                    //Vetor de documentos e linhas destes
                    VetorDocumentosBM25 = new string[Arquivos_Info.Count()][];

                    string[] splitAux = { " ", "<", ">", "/key", "/think" };


                    int cont = 0;
                    ListaLogs.Add("Carregando arquivos");
                    foreach (FileInfo Arquivo in Arquivos_Info.OrderBy(p => p.Name))    //Para cada arquivo
                    {
                        Porcentagem = (cont * 100 / Arquivos_Info.Count()).ToString();
                        string[] aux = TratamentoDeTexto.TransformacaoLexica(File.ReadAllText(Arquivo.FullName)).Split(splitAux, StringSplitOptions.RemoveEmptyEntries);
                        VetorDocumentosBM25[cont++] = aux;  //Carrega todas as linhas
                    }

                    ListaLogs.Add("Iniciando threads");

                    for (int i = IndiceBM25; i < tamanhovocabulario; i++)
                    {
                        Porcentagem = (i * 100 / tamanhovocabulario).ToString() + "%";

                        NovaThread[i] = new Thread(Threads_ConstruirMatrizBM);    //Thread para o arquivo
                        NovaThread[i].Priority = ThreadPriority.Highest;

                        NovaThread[i].Start();  //Inicia thread

                        Aux++;  //Incrementa contador

                        if (Aux >= NumMaximoThreads) //Controla quantidade de threads lancadas
                        {
                            ListaLogs.Add("Aguardando threads terminarem");
                            for (int j = Contador; j <= i; j++)  //Aguarda cada thread acabar
                                NovaThread[j].Join();

                            ListaLogs.Add("Escrevendo buffer");
                            for (int j = Contador; j <= i; j++)  //Aguarda cada thread acabar
                            {
                                File.WriteAllText(Configuracoes.Diretorio_ColecaoBM25() + "\\" + j + ".txt", BMMatriz[j], Encoding.UTF8);
                                BMMatriz[j] = null;
                            }

                            Aux = 0;
                            Contador += NumMaximoThreads;    //Controle de ultima thread que terminou o processamento

                            ListaLogs.Add("Iniciando threads");
                        }
                    }

                    Porcentagem = "100%";

                    ListaLogs.Add("Aguardando threads terminarem");
                    for (int j = Contador; j < tamanhovocabulario; j++)    //Aguarda as threads restantes acabarem
                        NovaThread[j].Join();

                    ListaLogs.Add("Escrevendo buffer");
                    for (int j = Contador; j < tamanhovocabulario; j++)  //Aguarda cada thread acabar
                    {
                        File.WriteAllText(Configuracoes.Diretorio_ColecaoBM25() + "\\" + j + ".txt", BMMatriz[j], Encoding.UTF8);
                        BMMatriz[j] = null;
                    }
                }

                VetorDocumentosBM25 = null;

                ListaLogs.Add("Agrupando vetores BM25");

                try
                {
                    for (int i = 0; i < tamanhovocabulario; i++)
                    {
                        Porcentagem = (i * 100 / tamanhovocabulario).ToString() + "%";

                        BMMatriz[i] = File.ReadAllText(Configuracoes.Diretorio_ColecaoBM25() + "\\" + i + ".txt", Encoding.UTF8);    //Adiciona na matriz
                    }

                    Porcentagem = "100%";
                }
                catch (Exception ex)
                {
                    ListaLogs.Add("Memória insuficiente");

                    BMMatriz = null;

                    RegistroDeExcessoes.Incluir(new Excessao("Erro ao agrupar vetores BM25", ex.ToString()));

                    ModuloFuncionando = false;

                    return;
                }

                try
                {
                    ListaLogs.Add("Escrevendo agrupamento de vetores de BM25");
                    //Escreve todas as linhas
                    File.WriteAllLines(Configuracoes.Caminho_VetoresBM25(), BMMatriz, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    RegistroDeExcessoes.Incluir(new Excessao("Erro ao escrever vetores BM25", ex.ToString()));

                    ModuloFuncionando = false;
                }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao escrever vetores BM25", ex.ToString()));

                ModuloFuncionando = false;
            }

            return;
        }
        #endregion
    }
}