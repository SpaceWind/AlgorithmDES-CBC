using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CBCDES
{
    public partial class Form1 : Form
    {
        List<string> eCode = new List<string>();
        List<string> dCode = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }
 
        /// <summary>
        /// Рассчет С-D
        /// </summary>
        /// <param name="sourceSequence"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        char[] shiftSequence(char[] sourceSequence, int shift)
        {
            char[] procSequence = new char[28];

            for (int j = 0; j < shift; j++)
            {
                for (int i = 0; i < 27; i++)
                {
                    procSequence[i] = sourceSequence[i + 1];
                }
                procSequence[27] = sourceSequence[0];
                sourceSequence = procSequence;
            }
            return procSequence;
        }

        /// <summary>
        /// Генерация 16 48-битных ключей
        /// </summary>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        char[] generateKey(char[] C, char[] D)
        {
            char[] CD = new char[56];
            char[] key = new char[48];
            int i,j;

            #region H-матрица завершающей обработки ключа
            int[,] Н = new int[,]{  {14,    17,   11,    24,    1,    5},
                                    { 3,    28,   15,     6,    21,   10},
                                    {23,    19,   12,     4,    26,    8},
                                    {16,     7,   27,    20,    13,    2},
                                    {41,    52,   31,    37,    47,   55},
                                    {30,    40,   51,    45,    33,   48},
                                    {44,    49,   39,    56,    34,   53},
                                    {46,    42,   50,    36,    29,   32}};
            #endregion

            CD = Enumerable.Concat(C, D).ToArray<char>();

            int z = 0;
            for (i = 0; i < 8; i++)
            {
                for (j = 0; j < 6; j++)
                {
                    key[z] = CD[Н[i, j]-1];
                    z++;
                }
            }

            return key;
        }

        /// <summary>
        /// Рассчет функции Фейстеля, вычисление L и R
        /// </summary>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        /*
            L(i) = R(i-1)
            R(i) = L(i-1) xor f(R(i-1), K(i)) 
        
            R(i-1) = L(i), i = 1, 2, ..., 16;
            L(i-1) = R(i) xor f(L(i), K(i))
        */
        char[] calculatedLR(char[] key, char[] R0, char[] L0, bool flag = false)
        {
            int i, j, z;
            char[] L1 = new char[32];
            char[] R1 = new char[32];

            ///////////////Инициализация таблиц S1 - S8///////////////////////////////
            #region Инициализация таблиц S1 - S8
            int[, ,] S = new int[,,] 
            {
                { 
                    { 14, 4, 13,  1,   2, 15,  11,  8,   3, 10,   6, 12,   5,  9,   0,  7 }, 
                    { 0, 15, 7,  4,  14,  2,  13,  1,  10,  6,  12, 11,   9,  5,   3,  8 }, 
                    { 4,  1, 14,  8,  13,  6,   2, 11,  15, 12,   9,  7,   3, 10,   5,  0 }, 
                    { 15, 12, 8,  2,   4,  9,   1,  7,   5, 11,   3, 14,  10,  0,   6, 13 }
                },
                {   
                    { 15,  1,   8, 14,   6, 11,   3,  4,   9,  7,   2, 13,  12,  0,   5, 10 }, 
                    { 3, 13,   4,  7,  15,  2,   8, 14,  12,  0,   1, 10,   6,  9,  11,  5 }, 
                    { 0, 14,   7, 11,  10,  4,  13,  1,   5,  8,  12,  6,   9,  3,   2, 15 }, 
                    { 13,  8,  10,  1,   3, 15,   4,  2,  11,  6,   7, 12,   0,  5,  14,  9 }
                },
                {   
                    { 10,  0,   9, 14,   6,  3,  15,  5,   1, 13,  12,  7,  11,  4,   2,  8 }, 
                    { 13,  7,   0,  9,   3,  4,   6, 10,   2,  8,   5, 14,  12, 11,  15,  1 }, 
                    { 13,  6,   4,  9,   8, 15,   3,  0,  11,  1,   2, 12,   5, 10,  14,  7 }, 
                    { 1, 10,  13,  0,   6,  9,   8,  7,   4, 15,  14,  3,  11,  5,  2, 12 }
                },
                {   
                    { 7, 13,  14,  3,   0,  6,   9, 10,   1,  2,  8,  5,  11, 12,   4, 15 }, 
                    { 13,  8,  11,  5,  6, 15,   0,  3,   4,  7,   2, 12,   1, 10,  14,  9 }, 
                    { 10,  6,   9,  0,  12, 11,   7, 13,  15,  1,   3, 14,   5,  2,  8,  4 }, 
                    { 3, 15,   0,  6,  10,  1, 13,  8,   9,  4,   5, 11,  12,  7,   2, 14 }
                },
                {   
                    { 2, 12,   4,  1,   7, 10,  11,  6,   8,  5,   3, 15,  13,  0,  14,  9 }, 
                    { 14, 11,   2, 12,   4,  7,  13,  1,   5,  0,  15, 10,   3,  9,   8,  6 }, 
                    { 4,  2,   1, 11,  10, 13,   7,  8,  15,  9,  12,  5,   6,  3,  0, 14 }, 
                    { 11,  8,  12,  7,   1, 14,   2, 13,   6, 15,   0,  9,  10,  4,   5,  3 }},
                {   
                    { 12,  1,  10, 15,   9,  2,  6,  8,   0, 13,   3,  4, 14,  7,  5, 11 }, 
                    { 10, 15,   4,  2,   7, 12,   9,  5,   6,  1,  13, 14,   0, 11,   3,  8 }, 
                    { 9, 14,  15,  5,   2,  8,  12,  3,   7,  0,   4, 10,   1, 13,  11,  6 }, 
                    { 4,  3,   2, 12,   9,  5,  15, 10,  11, 14,   1,  7,   6,  0,   8, 13 }},
                {   
                    { 4, 11,   2, 14,  15,  0,   8, 13,   3, 12,   9,  7,   5, 10,   6,  1 }, 
                    { 13,  0,  11,  7,   4,  9,   1, 10,  14,  3,  5, 12,   2, 15,   8,  6 }, 
                    { 1,  4,  11, 13,  12,  3,   7, 14,  10, 15,   6,  8,   0,  5,   9,  2 }, 
                    { 6, 11,  13,  8,   1,  4,  10,  7,   9,  5,   0, 15,  14,  2,   3, 12 }},
                {   
                    { 13,  2,   8,  4,   6, 15,  11,  1,  10,  9,   3, 14,   5,  0,  12,  7 }, 
                    { 1, 15,  13,  8,  10,  3,   7,  4,  12,  5,   6, 11,   0, 14,   9,  2 }, 
                    { 7, 11,   4,  1,   9, 12,  14,  2,   0,  6,  10, 13,  15,  3,   5,  8 }, 
                    { 2,  1,  14,  7,   4, 10,   8, 13,  15, 12,   9,  0,   3,  5,   6, 11 }
                }
            };
            #endregion

            ////////////////////////////////Функция расширитель Е, E(R0)/////////////////////////////////////
            int[,] E = new int[,] { {32,     1,    2,     3,     4,    5},
                                    {4,     5,    6,     7,    8,    9},
                                    {8,     9,   10,    11,    12,   13},
                                    {12,    13,   14,    15,    16,   17},
                                    {16,    17,   18,    19,    20,   21},
                                    {20,    21,   22,    23,    24,   25},
                                    {24,    25,   26,    27,    28,   29},
                                    {28,    29,   30,    31,    32,    1 }};

            char[] ER0 = new char[48];

            z = 0;
            for (i = 0; i < 8; i++)
            {
                for (j = 0; j < 6; j++)
                {
                    if (flag == false)
                    {
                        ER0[z] = R0[E[i, j]-1];
                    }
                    else
                    {
                        ER0[z] = L0[E[i, j]-1];
                    }
                    z++;
                }
            }

            ////////////////////////////////XOR ключа и E(R0)/////////////////////////////////////
            char[] xoredKey = new char[48];
            for (i = 0; i < 48; i++)
            {
                xoredKey[i] = (Convert.ToBoolean(ER0[i] ^ key[i]))?'1':'0';
            }

            ////////////////////////////////S1....8 блоки////////////////////////////////////
            char[,] B = new char[8, 6];

            for(i=z=0; i<8; i++)
            {
                for(j=0; j<6; j++)
                {
                    B[i, j] = xoredKey[z++];
                }
            }

            #region SBProcSequence
            ////////////////////////////////Получение S1(B1)S2(B2)S3(B3)S4(B4)S5(B5)S6(B6)S7(B7)S8(B8)/////////////////////////////////////
            
            string SBProcSequence = "";
            string strDecToBin;
            int rowSBlock, columnSBlock;

            for (i = 0; i < 8; i++)
            {
                rowSBlock = Convert.ToInt32(B[i, 0].ToString() + B[i, 5].ToString(), 2);
                columnSBlock = Convert.ToInt32(B[i, 1].ToString() +
                                                   B[i, 2].ToString() +
                                                   B[i, 3].ToString() +
                                                   B[i, 4].ToString(), 2);
                strDecToBin = Convert.ToString(S[i, rowSBlock, columnSBlock],2);

                while (strDecToBin.Length < 4)
                {
                    strDecToBin = "0" + strDecToBin;
                }

                SBProcSequence += strDecToBin;
            }
            #endregion

            /////////////////////////////////Финальная стадия расчёта f это выполнение перестановки SB_itog////////////////////////////
            //Функция перестановки P    
            int[,] P = new int[,] { {16,   7,  20,  21},
                                    {29,  12,  28,  17},
                                    {1,  15,  23,  26},
                                    {5,  18,  31,  10},
                                    {2,   8,  24,  14},
                                    {32,  27,   3,   9},
                                    {19,  13,  30,   6},
                                    {22,  11,   4,  25 }};

            char[] f = new char[32];

            for (i = z = 0; i < 8; i++)
            {
                for (j = 0; j < 4; j++)
                {
                    f[z++] = SBProcSequence[P[i, j]-1];
                }

            }

            /*            
            L(i) = R(i-1)
            R(i) = L(i-1) xor f(R(i-1), K(i)) 
        
            R(i-1) = L(i), i = 1, 2, ..., 16;
            L(i-1) = R(i) xor f(L(i), K(i))*/
            /////////////////////Получаем R1 = L0 xor f(R0, K1), L0 = R1 xor f(L1, K1)////////////////////////////////////////////////////////

            if (flag == false)
            {
                for (i = 0; i < 32; i++)
                {
                    R1[i] = (Convert.ToBoolean(L0[i] ^ f[i])) ? '1' : '0';
                }

                return R1;
            }
            else
            {
                for (i = 0; i < 32; i++)
                {
                    L1[i] = (Convert.ToBoolean(R0[i] ^ f[i])) ? '1' : '0';
                }

                return L1;
            }

        }

        /// <summary>
        /// Шифрация сообщения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private string[] getMessageText(string text)
        {
            string rawText = text;
            
            if(rawText.Length < 8)
            {
                MessageBox.Show("Длина блока текста должна быть > 8 символов");
                return new string[0];
            }
            while (rawText.Length % 8 != 0)
            {
                rawText += "\0";
            }
            string[] stringArray = new string[rawText.Length / 8];
            for (int i = 0, j = 0; i < stringArray.Count(); i++)
            {
                stringArray[i] = rawText.Substring(j, 8);
                j += 8;
            }
            return stringArray;
        }

        private char[] initialArrange(string array, bool direction = false)
        {
            char[] arrangedArray = new char[64];
            int[,] IP = new int[,] { {58,    50,   42,    34,    26,   18,    10,    2},
                                     {60,    52,   44,    36,    28,   20,    12,    4},
                                     {62,    54,   46,    38,    30,   22,    14,    6},
                                     {64,    56,   48,    40,    32,   24,    16,    8},
                                     {57,    49,   41,    33,    25,   17,     9,    1},
                                     {59,    51,   43,    35,    27,   19,    11,    3},
                                     {61,    53,   45,    37,    29,   21,    13,    5},
                                     {63,    55,   47,    39,    31,   23,    15,    7}};

            int k = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if(!direction)
                        arrangedArray[k++] = array[IP[i, j] - 1];
                    else
                        arrangedArray[IP[i, j] - 1] = array[k++];
                }
            }
            return arrangedArray;
        }
        
        private string getByteString(string textBlock)
        {
            byte[] byteArray = Encoding.GetEncoding(1251).GetBytes(textBlock);
            string procString, result = "";
            for(int i = 0; i < byteArray.Length; i++)
            {
                procString = Convert.ToString(byteArray[i], 2);
                while(procString.Length < 8)
                {
                    procString = "0" + procString;
                }
                result += procString;
            }
            return result.ToString();
        }

        private string getKeyByteString(string str)
        { 
            string key = str;
            if(key.Length != 8)
            {
                MessageBox.Show("Ключ для DES должен быть 8 бит");
                return null;
            }
            //byte[] byteKey = Encoding.GetEncoding(1251).GetBytes(key);

            return getByteString(key);
        }

        private char[] stringToCharArray(string str, int arraySize)
        {
            char[] result = new char[arraySize];
            for (int i = 0; i < arraySize; i++)
                result[i] = str[i];
            return result;
        }

        private string charToString(char[] array)
        {
            string result = "";
            foreach (char symbol in array)
                result += symbol.ToString();
            return result;
        }

        private char[] getInitialSequence(string textbox, string key)
        {
            string sequence = textbox;
                
            if (sequence.Length != 8)
            {
                return stringToCharArray(key, 64);
            }

            byte[] byteKey = Encoding.GetEncoding(1251).GetBytes(sequence);            
            string stringByteSequence = getByteString(sequence);

            return stringToCharArray(stringByteSequence, 64);
        }

        private char[,] generateDESKeys(string text)
        {
            char[, ,] CDSequences = new char[2, 17, 28];//[0,,] - C; [1,,] - D;

            string keyByteString = getKeyByteString(text);//key string
            if (keyByteString == null) return null;

            ///////////////Формирование С0 - D0//////////////////////////////////

            //K представляет собой 64-битовый блок с
            //восемью битами контроля по четности, расположенными в позициях 8,16,24,32,40,48,56,64.
            //C0: {57, 49, 41, 33, 25, 17, 9, 1, 58, 50, 42, 34, 26, 18, 10, 2, 59, 51, 43, 35, 27, 19, 11, 3, 60, 52, 44, 36}
            //D0: {63, 55, 47, 39, 31, 23, 15, 7, 62, 54, 46, 38, 30, 22, 14, 6 ,61, 53, 45, 37, 29, 21, 13, 5, 28, 20, 12, 4}
            
            int[] CIndexs = { 57, 49, 41, 33, 25, 17, 9, 1, 58, 50, 42, 34, 26, 18,
                                  10, 2, 59, 51, 43, 35, 27, 19, 11, 3, 60, 52, 44, 36};//28
            int[] DIndexs = { 63, 55, 47, 39, 31, 23, 15, 7, 62, 54, 46, 38, 30, 22,
                                  14, 6, 61, 53, 45, 37, 29, 21, 13, 5, 28, 20, 12, 4 };//28

            for (int i = 1, j = 0, t = 0; i <= 64; i++)
            {
                if (CIndexs.Contains(i))
                {
                    CDSequences[0, 0, j++] = keyByteString[i - 1];
                }
                else
                    if (DIndexs.Contains(i))
                    {
                        CDSequences[1, 0, t++] = keyByteString[i - 1];
                    }
            }
            ///////////////Формирование С1-16, D1-16//////////////////////////////
            int[] shiftsOrder = new int[] { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };
            char[] procCSequence = new char[28], procDSequence = new char[28];
            for (int i = 1, j = 0; j < 16; i++, j++)
            {
                for (int z = 0; z < 28; z++)
                { 
                    procCSequence[z] = CDSequences[0, i - 1, z];
                    procDSequence[z] = CDSequences[1, i - 1, z];
                }
                procCSequence = shiftSequence(procCSequence, shiftsOrder[j]);
                procDSequence = shiftSequence(procDSequence, shiftsOrder[j]);
                for (int z = 0; z < 28; z++)
                {
                    CDSequences[0, i, z] = procCSequence[z];
                    CDSequences[1, i, z] = procDSequence[z];
                }
            }

            ///////////////Инициализация 16 ключей///////////////////////////////
            char[,] DESKeys = new char[16, 48];
            char[] procKeySequence = new char[48];
            ///////////////Формирование 16 ключей///////////////////////////////
            for (int i = 0; i < 16; i++)
            {
                for (int z = 0; z < 28; z++)
                { 
                    procCSequence[z] = CDSequences[0, i + 1, z];
                    procDSequence[z] = CDSequences[1, i + 1, z];
                }
                procKeySequence = generateKey(procCSequence, procDSequence);
                for(int j = 0; j < 48; j++)
                {
                    DESKeys[i, j] = procKeySequence[j];
                }
            }
            return DESKeys;
        }

        private char[] encodingLoops(char[] rearrangedBlock)
        {
            char[,] DESKeys = generateDESKeys(textBox1.Text);
            if (DESKeys == null) return null;

            char[] LSequence = new char[32], 
                    RSequence = new char[32];
            for(int i = 0; i < 32; i++)
            {
                LSequence[i] = rearrangedBlock[i];
            }
            for(int i = 32; i < 64; i++)
            {
                RSequence[i-32] = rearrangedBlock[i];
            }

            //////////////////////////16 циклов шифрования/////////////////////////////////////
            char[] procKey = new char[48], loopSequence = new char[32];
            for(int loop = 0; loop < 16; loop++)
            {
                for (int i = 0; i < 48; i++)
                    procKey[i] = DESKeys[loop, i];
                loopSequence = calculatedLR(procKey, RSequence, LSequence);
                for (int i = 0; i < 32; i++)
                {
                    LSequence[i] = RSequence[i];
                    RSequence[i] = loopSequence[i];
                }
            }
            ////////////////////Слияние R16 и L16//////////////////////////////////
            char[] loopsResult = new char[64];

            for(int i = 0, j = 0, k = 0; i < 64; i++)
            {
                if(i < 32)
                {
                    loopsResult[i] = RSequence[j++];
                }
                else
                {
                    loopsResult[i] = LSequence[k++];
                }
            }
            return loopsResult;
        }

        private char[] decodingLoops(char[] loopsResult)
        {
            char[,] DESKeys = generateDESKeys(textBox2.Text);
            if (DESKeys == null) return null;

            char[] LSequence = new char[32],
                    RSequence = new char[32];
            for (int i = 0; i < 32; i++)
            {
                RSequence[i] = loopsResult[i];
            }
            for (int i = 32; i < 64; i++)
            {
                LSequence[i - 32] = loopsResult[i];
            }

            char[] procKey = new char[48], loopSequence = new char[32];
            for (int loop = 15; loop >= 0 ; loop--)
            {
                for (int i = 0; i < 48; i++)
                    procKey[i] = DESKeys[loop, i];
                loopSequence = calculatedLR(procKey, RSequence, LSequence, true);
                for (int i = 0; i < 32; i++)
                {
                    RSequence[i] = LSequence[i];
                    LSequence[i] = loopSequence[i];
                }
            }
            char[] rearrangedBlock = new char[64];

            for (int i = 0, j = 0, k = 0; i < 64; i++)
            {
                if (i < 32)
                {
                    rearrangedBlock[i] = LSequence[j++];
                }
                else
                {
                    rearrangedBlock[i] = RSequence[k++];
                }
            }
            return rearrangedBlock;
        }

        private char[] finalRearrange(char[] byteCode, bool direction = false)
        { 
            int[,] FP = new int[,] {    {40,     8,   48,    16,    56,   24,    64,   32 },
                                        {39,     7,   47,    15,    55,   23,    63,   31 },
                                        {38,     6,   46,    14,    54,   22,    62,   30 },
                                        {37,     5,   45,    13,    53,   21,    61,   29 },
                                        {36,     4,   44,    12,    52,   20,    60,   28 },
                                        {35,     3,   43,    11,    51,   19,    59,   27 },
                                        {34,     2,   42,    10,    50,   18,    58,   26 },
                                        {33,     1,   41,     9,    49,   17,    57,   25 }};

            char[] result = new char[64];

            for(int i = 0, k = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    if(!direction)
                        result[k++] = byteCode[FP[i, j] - 1];
                    else
                        result[FP[i, j] - 1] = byteCode[k++];
                }
            }
            return result;
        }

        private char[] encodeBlock(string textBlock)
        {
            char[] rearrangedBlock = initialArrange(textBlock);
            char[] loopsResult = encodingLoops(rearrangedBlock);
            if (loopsResult == null) return null;

            return finalRearrange(loopsResult);
        }

        private char[] decodeBlock(string textBlock)
        {
            char[] loopsResult = finalRearrange(stringToCharArray(getByteString(textBlock), 64), true);
            char[] byteMessageBlock = initialArrange(charToString(decodingLoops(loopsResult)), true);

            return byteMessageBlock;
        }

        private void button1_Click(object sender, EventArgs e)//зашифровать
        {
            ///////////Считывание исходного текста/////////////////////////////////////
            string[] messageTextBlocks = getMessageText(richTextBox1.Text);
            if (messageTextBlocks.Count() == 0) return;
            ///////////Шифрование блоков текста по 64 бит///////////////////
            char[] cypher = new char[64], xoredTextBlock = new char[64];
            char[] initialSequence = getInitialSequence(textBox3.Text, getKeyByteString(textBox1.Text));
            richTextBox3.Text = "";
            foreach(string textBlock in messageTextBlocks)
            {
                if(textBlock == null)
                    break;

                xoredTextBlock = stringToCharArray(getByteString(textBlock),64);
                for (int i = 0; i < 64; i++)
                    xoredTextBlock[i] = (Convert.ToBoolean(xoredTextBlock[i] ^ initialSequence[i])) ? '1' : '0';

                cypher = encodeBlock(charToString(xoredTextBlock));
                if(cypher == null) return;

                eCode.Add(charToString(cypher));
                for (int i = 0; i < 64; i++)
                    initialSequence[i] = cypher[i];

                ////////////////Конвертация из байт в символы//////////////////////////
                byte[] cypherByteCode = new byte[8];
                string[] sypherByteBlocks = new string[8];

                for(int i = 0, j = 0; i < 64; i++)
                {
                    if(i > 7 && i % 8 == 0)
                        j++;

                    sypherByteBlocks[j] += cypher[i];
                }
                
                for(int i = 0; i < 8; i++)
                {
                    cypherByteCode[i] = Convert.ToByte(Convert.ToInt32(sypherByteBlocks[i], 2));
                }
                richTextBox3.Text += Encoding.GetEncoding(1251).GetString(cypherByteCode);
            }
        }

        /// <summary>
        /// Функция расшифровки сообщения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            ///////////////////////Считывание исходного текста/////////////////////////////////////
            
            string[] messageTextBlocks = getMessageText(richTextBox2.Text);
            if (messageTextBlocks.Count() == null) return;

            ///////////Разбиение всего текста на участки по 64 бита///////////////////
            char[] byteMessageBlock = new char[64], tempByteCode = new char[64];
            char[] initialSequence = getInitialSequence(textBox4.Text, getKeyByteString(textBox2.Text));
            richTextBox4.Text = "";
            foreach(string textBlock in messageTextBlocks)
            {
                if (textBlock == null)
                    break;
                
                byteMessageBlock = decodeBlock(textBlock);

                for (int i = 0; i < 64; i++)
                    byteMessageBlock[i] = (Convert.ToBoolean(byteMessageBlock[i] ^ initialSequence[i])) ? '1' : '0';
                
                initialSequence = stringToCharArray(getByteString(textBlock), 64);dCode.Add(charToString(initialSequence));
                ////////////////Конвертация из байт в символы//////////////////////////
                byte[] messageByteCode = new byte[8];
                string[] messageByteBlocks = new string[8];

                for (int i = 0, j = 0; i < 64; i++)
                {
                    if (i > 7 && i % 8 == 0)
                        j++;

                    messageByteBlocks[j] += byteMessageBlock[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    messageByteCode[i] = Convert.ToByte(Convert.ToInt32(messageByteBlocks[i], 2));
                }

                string partOfMessage = Encoding.GetEncoding(1251).GetString(messageByteCode);
                richTextBox4.Text += partOfMessage;
            }
        }

        //load text
        private void button6_Click(object sender, EventArgs e)
        {
            string readFile;
            string filePath;

            filePath = System.Windows.Forms.Application.StartupPath;
            filePath = filePath + "\\text.txt";
            readFile = System.IO.File.ReadAllText(filePath, Encoding.GetEncoding(1252));

            richTextBox1.Text = readFile;
        }

        /// <summary>
        /// Сохранение в файл зашифрованного текста и ключей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            string writeFile, filePathText, filePathKeys;
            string keys = "";
            string writeKeys = "";
            int i;

            keys = textBox1.Text;

            for (i = 0; i < keys.Length; i++)
            {
                if (i % 8 == 0 && i > 7)
                    writeKeys += "\r\n";

                writeKeys += keys[i];
            }

            filePathKeys = System.Windows.Forms.Application.StartupPath;
            filePathKeys = filePathKeys + "\\keys.txt";
            System.IO.File.WriteAllText(filePathKeys, writeKeys, Encoding.GetEncoding(1252));

            MessageBox.Show("ключи записаны в файл");
        }

        /// <summary>
        /// Загрузка ключей и текста из файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            string readFile, filePathText, filePathKeys;
            string readKeys = "";
            string key1;
            int i, flag=0;

            /*filePathText = System.Windows.Forms.Application.StartupPath;
            filePathText = filePathText + "\\encodingText.txt";
            readFile = System.IO.File.ReadAllText(filePathText, System.Text.Encoding.GetEncoding(1251));
            richTextBox2.Text = readFile;*/

            filePathKeys = System.Windows.Forms.Application.StartupPath;
            filePathKeys = filePathKeys + "\\keys.txt";
            readKeys = System.IO.File.ReadAllText(filePathKeys, Encoding.GetEncoding(1252));

            for (i = 0; i < readKeys.Length; i++)
            {
                if (readKeys[i] == '\r' && readKeys[i + 1] == '\n')
                { flag++; i += 1; continue; } 

                if (flag == 0)
                    textBox2.Text += readKeys[i];

                //if (flag == 1)
                    //textBox5.Text += readKeys[i];

                //if (flag == 2)
                    //textBox6.Text += readKeys[i];
            }
        }
        
        /// <summary>
        /// Генерация ключей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {

            Random rand = new Random();
            int i;
            string key = "";
            int[] cc = new int[8];
            byte[] bytes2 = new byte[8];

            for (i = 0; i < 8; i++)
            {
                cc[i] = rand.Next(255);

                while (cc[i] == 10 || cc[i] == 13)//??
                    cc[i] = rand.Next(255);
            }

            for (i = 0; i < 8; i++)
                bytes2[i] = Convert.ToByte(cc[i]);

            key += Encoding.GetEncoding(1251).GetString(bytes2);
            textBox1.Text = key;

            key = "";
            for (i = 0; i < 8; i++)
            {
                cc[i] = rand.Next(255);

                while (cc[i] == 10 || cc[i] == 13)
                    cc[i] = rand.Next(255);
            }

            for (i = 0; i < 8; i++)
                bytes2[i] = Convert.ToByte(cc[i]);

            key += Encoding.GetEncoding(1251).GetString(bytes2);
            //textBox4.Text = key;

            key = "";
            for (i = 0; i < 8; i++)
            {
                cc[i] = rand.Next(255);

                while (cc[i] == 10 || cc[i] == 13)
                    cc[i] = rand.Next(255);
            }

            for (i = 0; i < 8; i++)
                bytes2[i] = Convert.ToByte(cc[i]);

            key += Encoding.GetEncoding(1251).GetString(bytes2);
            //textBox3.Text = key;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string readFile, filePathText, filePathKeys;
            string readKeys = "";
            string key1;
            int i, flag = 0;

            filePathText = System.Windows.Forms.Application.StartupPath;
            filePathText = filePathText + "\\encodingText.txt";
            readFile = System.IO.File.ReadAllText(filePathText, Encoding.GetEncoding(1252));
            richTextBox2.Text = readFile;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string writeFile, filePathText;
            writeFile = richTextBox3.Text;

            filePathText = System.Windows.Forms.Application.StartupPath;
            filePathText = filePathText + "\\encodingText.txt";
            System.IO.File.WriteAllText(filePathText, writeFile, Encoding.GetEncoding(1252));

            MessageBox.Show("Зaкодированный текст записан в файл");
        }
    }
}
