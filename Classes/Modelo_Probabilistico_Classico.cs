using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections;

namespace TCC
{
    /// <summary>
    /// Classe para modelo probabilistico
    /// </summary>
    class Modelo_Probabilistico_Classico
    {
        private static Queue<string> Documentos = new Queue<string>();  //Documentos do diretorio
        private static readonly object ReadOnlyObject = new object();   //Objeto para bloqueio de recursos
        private static readonly object ReadOnlyObject2 = new object();   //Objeto para bloqueio de recursos
        private static readonly object ReadOnlyObject3 = new object();   //Objeto para bloqueio de recursos
        private static bool ModuloFuncionando;   //Indica que o modulo esta funcionando
        private static DirectoryInfo Diretorio_Documentos;  //Informacoes do diretorio
        private static FileInfo[] Arquivos_Info;  //Informacoes dos arquivos
        private static string[] Arquivo;
        private static List<string> Vocabulario;   //Lista de palavras do vocabulario
        private static int QuantidadeDocumentos;     //Quantidade de documentos no diretorio
        private SortedDictionary<string, double> DocumentoseRelevantes;    //Colecao de arquivos com nome e relevancia
        private static List<BitArray> listaBinaria; //Colecao de bits

        private static List<string> ListaLogs = new List<string>();
        private static string Porcentagem = "Aguardando Processo";

        /// <summary>
        /// Novo objeto Modelo_Probabilistico_Classico
        /// </summary>
        public Modelo_Probabilistico_Classico()
        {
            ModuloFuncionando = true;

            try
            {
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos           
                Vocabulario = new List<string>();   //Lista de palavras do vocabulario
                QuantidadeDocumentos = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories).Count();     //Quantidade de documentos no diretorio

                //Carrega vocabulario
                if (Vocabulario.Count() == 0)
                    Vocabulario.AddRange(File.ReadAllLines(Configuracoes.Caminho_Vocabulario()));

                if (listaBinaria == null)
                    CarregarVetores();

                if (Arquivo == null)
                    CarregarListaArquivos();

            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro na inicialização do modelo", ex.ToString()));

                ModuloFuncionando = false;
            }
        }

        /// <summary>
        /// Informacoes de status do modelo
        /// </summary>
        public static List<string> Logs()
        {
            return ListaLogs;
        }

        /// <summary>
        /// Informacoes sobre porcentagem de conclusao de processamento
        /// </summary>
        public static string PorcentagemDeConclusao()
        {
            return Porcentagem;
        }

        /// <summary>
        /// Indica funcionamento do modulo
        /// </summary>
        public static bool ModuloEmFuncionamento()
        {
            //Se a matriz nao estiver vazia e se o modulo nao encontrou erros
            if (/*Matriz != null &&*/ ModuloFuncionando && Arquivo != null && listaBinaria.Count() != 0)
                return true;    //Funcionando
            else
                return false;   //Nao esta funcionando
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

        /// <summary>
        /// Consulta base de de dados utilizando o modelo probabilistico
        /// </summary>
        public SortedDictionary<string, double> Consultar(string Frase)
        {
            try
            {
                Frase = TratamentoDeTexto.Eliminacao_Stopwords(Frase);

                DocumentoseRelevantes = new SortedDictionary<string, double>();    //Colecao de arquivos com nome e relevancia
                //if (VocabularioContem(Termo))    //Se o vocabulario contem o termo
                //{
                foreach (string Termo in Frase.Split(' '))
                    if (Vocabulario.Contains(Termo))    //Se o vocabulario contem o termo
                    {
                        //int Indice = Vocabulario.IndexOf(TermoDoVocabulario);    //Indice do termo no vocabulario

                        int Indice = Vocabulario.IndexOf(Termo);    //Indice do termo no vocabulario

                        int n = 0;  //Quantidade de documentos que contem o termo

                        for (int i = 0; i < listaBinaria[Indice].Length; i++) //Percorre Matriz
                            if (listaBinaria[Indice][i] == true)   //Se contem o termo no documento
                                n++;    //Incrementa o numero de documentos que contem o termo

                        //Calcula peso com base na formaula do modelo probabilistico
                        double Peso = Math.Log((QuantidadeDocumentos + 0.5) / (n + 0.5), 2);

                        for (int i = 0; i < listaBinaria[Indice].Length; i++) //Para cada elemento de matriz
                            if (listaBinaria[Indice][i] == true)   //Se o termo existe no arquivo
                                try
                                {
                                    if (DocumentoseRelevantes.ContainsKey(Arquivo[i]))  //Se o documento ja estiver na colecao
                                        DocumentoseRelevantes[Arquivo[i]] = double.Parse(DocumentoseRelevantes[Arquivo[i]].ToString()) + Peso;  //Incrementa o peso
                                    else
                                        DocumentoseRelevantes.Add(Arquivo[i], Peso);    //Armazena o peso
                                }
                                catch (Exception)
                                {
                                    ModuloFuncionando = false;
                                }
                    }
                    else if (VocabularioContem(Termo))
                    {
                        int Indice = Vocabulario.IndexOf(TermoDoVocabulario);    //Indice do termo no vocabulario

                        int n = 0;  //Quantidade de documentos que contem o termo

                        for (int i = 0; i < listaBinaria[Indice].Length; i++) //Percorre Matriz
                            if (listaBinaria[Indice][i] == true)   //Se contem o termo no documento
                                n++;    //Incrementa o numero de documentos que contem o termo

                        //Calcula peso com base na formaula do modelo probabilistico
                        double Peso = Math.Log((QuantidadeDocumentos + 0.5) / (n + 0.5), 2);

                        for (int i = 0; i < listaBinaria[Indice].Length; i++) //Para cada elemento de matriz
                            if (listaBinaria[Indice][i] == true)   //Se o termo existe no arquivo
                                try
                                {
                                    if (DocumentoseRelevantes.ContainsKey(Arquivo[i]))  //Se o documento ja estiver na colecao
                                        DocumentoseRelevantes[Arquivo[i]] = double.Parse(DocumentoseRelevantes[Arquivo[i]].ToString()) + Peso;  //Incrementa o peso
                                    else
                                        DocumentoseRelevantes.Add(Arquivo[i], Peso);    //Armazena o peso
                                }
                                catch (Exception ex)
                                {
                                    RegistroDeExcessoes.Incluir(new Excessao("Erro no meodelo probabilístico clássico", ex.ToString()));
                                    ModuloFuncionando = false;
                                }
                    }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro no modelo probabiístico clássico", ex.ToString()));
                ModuloFuncionando = false;
                DocumentoseRelevantes = null;
            }

            return DocumentoseRelevantes;   //Documentos relevantes encontrados
        }

        /// <summary>
        /// Carrega a colecao de documentos
        /// </summary>
        public static void CarregarListaArquivos()
        {
            ListaLogs.Add("");
            try
            {
                Arquivo = new string[Arquivos_Info.Count()];

                int cont = 0;

                //Adiciona cada arquivo do diretorio no vetor, ordenados pelo nome
                foreach (FileInfo item in Arquivos_Info.OrderBy(p => p.Name))
                {
                    Porcentagem = (cont * 100 / Arquivos_Info.Count()).ToString() + "%";

                    Arquivo[cont++] = item.FullName;
                }

                Porcentagem = "100%";
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao carregar lista de arquivos", ex.ToString()));

                ModuloFuncionando = false;
            }
        }

        /// <summary>
        /// Carrega vetores binarios
        /// </summary>
        public static void CarregarVetores()
        {
            listaBinaria = new List<BitArray>();
            List<bool> encodedSource;
            BitArray bits;

            try
            {
                ListaLogs.Add("|Carregando vetores MPC                                   |");
                StreamReader sr = new StreamReader(Configuracoes.Caminho_VetoresBinarios());

                while (!sr.EndOfStream)
                {
                    encodedSource = new List<bool>();

                    string source = sr.ReadLine();

                    for (int i = 0; i < source.Length; i++)
                    {
                        if (source[i] == '0')
                            encodedSource.Add(false);
                        else
                            encodedSource.Add(true);
                    }

                    bits = new BitArray(encodedSource.ToArray());

                    listaBinaria.Add(bits);
                }

                sr.Close();

                ListaLogs.Add("|Vetores carregados - - - - - OK                          |");
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao carregar vetores binários", ex.ToString()));

                ModuloFuncionando = false;
            }

            return;
        }

        /// <summary>
        /// Refaz visao logica, vocabulario e vetores binarios
        /// </summary>
        public static void Reconstruir()
        {
            try
            {
                VerificarDiretorios();

                //DirectoryInfo Diretorio = new DirectoryInfo(Configuracoes.Diretorio_VisaoLogica());
                //Diretorio.Delete(true);
                //File.Delete(Configuracoes.Caminho_Vocabulario());
                //File.Delete(Configuracoes.Caminho_VetoresBinarios());

                VerificarDiretorios();

                Documentos = new Queue<string>();  //Documentos do diretorio
                Diretorio_Documentos = new DirectoryInfo(Configuracoes.Diretorio_Documentos());    //Informacoes do diretorio
                Arquivos_Info = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories);  //Informacoes dos arquivos
                Arquivo = new string[Arquivos_Info.Count()];
                Vocabulario = new List<string>();   //Lista de palavras do vocabulario
                QuantidadeDocumentos = Diretorio_Documentos.GetFiles("*.txt", SearchOption.AllDirectories).Count();     //Quantidade de documentos no diretorio

                //Construir_VisaoLogica();
                //Construir_Vocabulario();
                Construir_VetoresBinarios();
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar reconstruir elementos do modelo", ex.ToString()));
                ModuloFuncionando = false;
            }

            return;
        }

        /// <summary>
        /// Verifica se as pastas necessarias existem
        /// </summary>
        private static bool VerificarDiretorios()
        {
            try
            {
                //Verifica se todas pastas existem e as criam caso nao existirem
                if (!File.Exists(Configuracoes.Diretorio_MP() + "DOCUMENTOS"))
                    Directory.CreateDirectory(Configuracoes.Diretorio_MP() + "DOCUMENTOS");

                if (!File.Exists(Configuracoes.Diretorio_MP() + "VISAO LOGICA"))
                    Directory.CreateDirectory(Configuracoes.Diretorio_MP() + "VISAO LOGICA");

                return true;
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar identificar diretórios", ex.ToString()));

                return false;
            }
        }

        #region Construi_MPC
        /// <summary>
        /// Threads
        /// </summary>
        private static void Threads_VisaoLogica()
        {
            List<string> Vocabulario_Arquivo = new List<string>();
            string ArquivoAtual;

            lock (ReadOnlyObject)   //Bloqueia acesso a pilha Documentos
            {
                //Le e retira o documento da pilha
                ArquivoAtual = Documentos.Peek();
                Documentos.Dequeue();
            }

            try
            {

                StreamReader sr = new StreamReader(ArquivoAtual, Encoding.UTF8); //Novo leitor

                while (!sr.EndOfStream)
                {
                    string[] splitAux = { " ", "<", ">", "/key", "/think" };
                    string[] Linha = TratamentoDeTexto.TransformacaoLexica(sr.ReadLine()).Split(splitAux, StringSplitOptions.RemoveEmptyEntries); //Divide as palavras

                    foreach (string Palavra in Linha)   //Para cada palavra
                        if (!Vocabulario_Arquivo.Contains(Palavra) && Palavra != "" && Palavra != null) //Verifica se ja existe na lista
                            Vocabulario_Arquivo.Add(Palavra);   //Adiciona na lista
                }

                sr.Close(); //Fecha arquivo

                Vocabulario_Arquivo.Sort(); //Ordena vocabulario

                string[] aux = ArquivoAtual.Split('\\');

                string caminho = Configuracoes.Diretorio_VisaoLogica() + "\\" + aux[aux.Count() - 1];


                //Escreve arquivo de visao logica do arquivo
                File.WriteAllLines(caminho, Vocabulario_Arquivo, Encoding.UTF8);

                ModuloFuncionando = false;

            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar ler, escrever e processar arquivo", ex.ToString()));
            }

            return;
        }

        /// <summary>
        /// Constroi visao logica
        /// </summary>
        private static void Construir_VisaoLogica()
        {
            try
            {
                foreach (FileInfo Arquivo in Arquivos_Info)      //Empilha arquivos nao pilha
                    Documentos.Enqueue(Arquivo.FullName);

                int QuantDocumentos = Documentos.Count();   //Quantidade de documentos

                Thread[] NovaThread = new Thread[QuantDocumentos];  //Cria uma thread para cada documento

                //Variaveis auxiliares para controle de threads lancadas
                int Aux = 0;
                int Contador = 0;
                int NumMaximoThreads = 5;

                //Dispara NumMaximoThreads threads de cada vez
                for (int i = 0; i < QuantDocumentos; i++)
                {
                    NovaThread[i] = new Thread(Threads_VisaoLogica);    //Thread para o arquivo

                    NovaThread[i].Start();  //Inicia thread

                    Aux++;  //Incrementa contador

                    if (Aux >= NumMaximoThreads) //Controla quantidade de threads lancadas
                    {
                        for (int j = Contador; j < i; j++)  //Aguarda cada thread acabar
                            NovaThread[j].Join();

                        Aux = 0;
                        Contador += NumMaximoThreads;    //Controle de ultima thread que terminou o processamento
                    }
                }

                for (int j = Contador; j < QuantDocumentos; j++)    //Aguarda as threads restantes acabarem
                    NovaThread[j].Join();
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar contruir visão lógica", ex.ToString()));
            }

            return;
        }

        /// <summary>
        /// Constroi vocabulario
        /// </summary>
        private static void Construir_Vocabulario()
        {
            try
            {
                //Informacoes sobre o diretorio de visao logica
                DirectoryInfo DiretorioVL = new DirectoryInfo(Configuracoes.Diretorio_VisaoLogica());

                //Informacoes sobre os arquivos de visao logica
                FileInfo[] ArquivosVL = DiretorioVL.GetFiles("*.txt", SearchOption.AllDirectories);

                StreamReader sr;    //Novo leitor

                List<string> Lista_Vocabulario = new List<string>();    //Lista de vocabulario
                string Linha;   //Linha lida de um arquivo

                foreach (FileInfo Arquivo in ArquivosVL)    //Para cada arquivo
                {
                    sr = new StreamReader(Arquivo.FullName, Encoding.UTF8); //Abre arquivo

                    while (!sr.EndOfStream) //Enquanto o arquivo nao terminar
                    {
                        Linha = sr.ReadLine();  //Le linha

                        if (!Lista_Vocabulario.Contains(Linha) && Linha != "" && Linha != null) //Se nao existir na linha
                            Lista_Vocabulario.Add(Linha);   //Adiciona palavra na lista
                    }

                    sr.Close(); //Fecha o arquivo
                }

                Lista_Vocabulario.Sort();   //Ordena a lista

                try
                {
                    //Escreve as palavras no arquivo
                    File.WriteAllLines(Configuracoes.Caminho_Vocabulario(), Lista_Vocabulario, Encoding.UTF8);
                }
                catch (Exception)
                {
                    ModuloFuncionando = false;
                }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar contruir vocabulário", ex.ToString()));
            }

            return;
        }

        #region Variaveis_Threads_VetoresBinarios
        //Carrega vocabulario
        static string[] vb_Vocabulario;

        //Vetor de documentos e linhas destes
        static string[][] VetorDocumentos;

        static int Indice = 0;
        #endregion

        private static void Threads_VetoresBinarios()
        {
            int i;

            lock (ReadOnlyObject2)   //Bloqueia acesso a pilha Documentos
            {
                i = Indice;
                Indice++;
            }

            try
            {
                string aux = "";

                for (int j = 0; j < VetorDocumentos.Count(); j++) //Para cada documento
                    if (VetorDocumentos[j].Contains(vb_Vocabulario[i]))    //Se existe o termo no documento
                        aux += "1"; //Existe o termo
                    else
                        aux += "0"; //Nao existe o termo

                //vb_Matriz[i] = aux;    //Adiciona na matriz

                //Escreve todas as linhas
                File.WriteAllText(Configuracoes.Diretorio_ColecaoVB() + "\\" + i + ".txt", aux, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar vetor binário", ex.ToString()));

                ModuloFuncionando = false;
            }

            return;
        }

        /// <summary>
        /// Constroi vetores binarios
        /// </summary>
        private static void Construir_VetoresBinarios()
        {
            try
            {
                //Informacoes sobre o diretorio de visao logica
                DirectoryInfo vb_DiretorioVL = new DirectoryInfo(Configuracoes.Diretorio_VisaoLogica());

                //Informacoes sobre os arquivos de visao logica
                FileInfo[] vb_ArquivosVL = vb_DiretorioVL.GetFiles("*.txt", SearchOption.AllDirectories);

                //Carrega vocabulario
                vb_Vocabulario = File.ReadAllLines(Configuracoes.Caminho_Vocabulario());

                //Vetor de documentos e linhas destes
                VetorDocumentos = new string[vb_ArquivosVL.Count()][];
                //vb_ArquivosVL = null;

                //Matriz para vetores binarios
                string[] vb_Matriz = new string[vb_Vocabulario.Count()];

                //Contador auxiliar
                int Contador = 0;
                int Aux = 0;

                int NumMaximoThreads = 100;
                Thread[] NovaThread = new Thread[vb_Vocabulario.Count()];  //Cria uma thread para cada documento            
                int tamanhovocabulario = vb_Vocabulario.Count();

                DirectoryInfo D = new DirectoryInfo(Configuracoes.Diretorio_ColecaoVB());
                FileInfo[] AD = D.GetFiles("*.txt", SearchOption.AllDirectories);

                Indice = AD.Count();
                Contador = Indice;

                vb_DiretorioVL = null;

                if (Indice != tamanhovocabulario)
                {
                    int cont = 0;
                    ListaLogs.Add("Carregando arquivos");
                    foreach (FileInfo Arquivo in vb_ArquivosVL)    //Para cada arquivo
                        VetorDocumentos[cont++] = File.ReadAllLines(Arquivo.FullName);  //Carrega todas as linhas

                    ListaLogs.Add("Iniciando threads");


                    for (int i = AD.Count(); i < tamanhovocabulario; i++)
                    {
                        Porcentagem = (i * 100 / tamanhovocabulario).ToString() + "%";

                        NovaThread[i] = new Thread(Threads_VetoresBinarios);    //Thread para o arquivo
                        NovaThread[i].Priority = ThreadPriority.Highest;

                        NovaThread[i].Start();  //Inicia thread

                        Aux++;  //Incrementa contador

                        if (Aux >= NumMaximoThreads) //Controla quantidade de threads lancadas
                        {
                            ListaLogs.Add("Aguardando threads terminarem");
                            for (int j = Contador; j < i; j++)  //Aguarda cada thread acabar
                                NovaThread[j].Join();

                            Aux = 0;
                            Contador += NumMaximoThreads;    //Controle de ultima thread que terminou o processamento

                            ListaLogs.Add("Iniciando threads");
                        }
                    }

                    Porcentagem = "100%";

                    ListaLogs.Add("Aguardando threads terminarem");
                    for (int j = Contador; j <= vb_Vocabulario.Count(); j++)    //Aguarda as threads restantes acabarem
                        NovaThread[j].Join();
                }

                vb_Vocabulario = null;

                ListaLogs.Add("Agrupando vetores binários");

                try
                {
                    for (int i = 0; i < tamanhovocabulario; i++)
                    {
                        Porcentagem = (i * 100 / tamanhovocabulario).ToString() + "%";

                        vb_Matriz[i] = File.ReadAllText(Configuracoes.Diretorio_ColecaoVB() + "\\" + i + ".txt", Encoding.UTF8);    //Adiciona na matriz
                    }

                    Porcentagem = "100%";
                }
                catch (Exception ex)
                {
                    ListaLogs.Add("Memória insuficiente");
                    ModuloFuncionando = false;

                    vb_Matriz = null;

                    RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar agrupar os vetores binários", ex.ToString()));

                    return;
                }

                try
                {
                    ListaLogs.Add("Escrevendo agrupamento de vetores binários");
                    //Escreve todas as linhas
                    File.WriteAllLines(Configuracoes.Caminho_VetoresBinarios(), vb_Matriz, Encoding.UTF8);

                    vb_Matriz = null;
                }
                catch (Exception ex)
                {
                    RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar escrever agrupamento de vetores binários", ex.ToString()));

                    vb_Matriz = null;
                    ModuloFuncionando = false;
                }
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao tentar criar os vetores binários", ex.ToString()));
            }

            return;
        }
        #endregion
    }
}