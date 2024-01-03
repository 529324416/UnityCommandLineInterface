using System;

namespace RedSaw.CommandLineInterface{

    public static class CLIUtils{

        /// <summary>calculate distance between two strings</summary>
        /// <param name="q">string 1</param>
        /// <param name="option">string 2</param>
        public static int LevenshteinDistance(string q, string option){
            
            int[,] matrix = new int[q.Length + 1, option.Length + 1];
            for (int i = 0; i <= q.Length; i++){
                for (int j = 0; j <= option.Length; j++){
                    if (i == 0){
                        matrix[i, j] = j;
                    }
                    else if (j == 0){
                        matrix[i, j] = i;
                    }else{
                        int cost = (q[i - 1] == option[j - 1]) ? 0 : 1;
                        matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                    }
                }
            }
            return matrix[q.Length, option.Length];
        }

        public static int SimpleFilter(string q, string option){

            if(option.Contains(q)){
                return q.Length;
            }
            return 0;
        }
    }
}