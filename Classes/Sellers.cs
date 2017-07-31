using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCC
{
    /// <summary>
    /// Classe que contem o algoritmo de Sellers
    /// </summary>
    static class Sellers
    {
        /// <summary>
        /// Algoritmo de Sellers
        /// </summary>
        private static bool editDistance(string p_Padrao, string p_Linha, int p_Erros)
        {
            try
            {
                int lact = 0;
                int pC = 0;
                int nC = 0;
                char y;

                int v_TamanhoPadrao = p_Padrao.Length;
                int p_TamanhoTexto = p_Linha.Length;
                bool v_ImprimeLinha = false;
                int v_NumVezesLinha = 0;
                int[] C = new int[200];

                for (int k = 0; k <= v_TamanhoPadrao; k++)
                    C[k] = k;

                if (lact == 0)
                    lact = p_Erros + 1;

                for (int pos = 1; pos <= p_TamanhoTexto; pos++)
                {
                    pC = 0;
                    nC = 0;
                    y = p_Linha[pos - 1];

                    if (y > 0)
                    {
                        for (int i = 1; i <= lact; i++)
                        {
                            if (p_Padrao[i - 1] == y)
                                nC = pC;
                            else
                            {
                                if (pC < nC)
                                    nC = pC;

                                if (C[i] < nC)
                                    nC = C[i];

                                nC++;
                            }

                            pC = C[i];
                            C[i] = nC;
                        }

                        while (C[lact] > p_Erros)
                            lact--;

                        if (lact == v_TamanhoPadrao)
                        {
                            v_ImprimeLinha = true;
                            v_NumVezesLinha++;
                        }
                        else
                            lact++;
                    }
                }

                if (v_ImprimeLinha)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                RegistroDeExcessoes.Incluir(new Excessao("Erro ao fazer casamento aproximado", ex.ToString()));
            }

            return false;
        }

        /// <summary>
        /// Buscar casamento
        /// </summary>
        public static bool Comparar(string Termo1, string Termo2)
        {
            int numeroDeErros = 0;

            if (Termo1.Length > Termo2.Length)
            {
                numeroDeErros = (int)(Termo2.Length * 0.2); //Define maximo de 20% de erro

                return editDistance(Termo1, Termo2, numeroDeErros);
            }
            else
            {
                numeroDeErros = (int)(Termo1.Length * 0.2); //Define maximo de 20% de erro

                return editDistance(Termo2, Termo1, numeroDeErros);
            }
        }
    }
}