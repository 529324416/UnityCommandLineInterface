using System;

namespace RedSaw.CommandLineInterface{

    public static class CLIUtils{

        static int GetEditDistance(string X, string Y)
        {
            int m = X.Length;
            int n = Y.Length;
    
            int[][] T = new int[m + 1][];
            for (int i = 0; i < m + 1; ++i) {
                T[i] = new int[n + 1];
            }
    
            for (int i = 1; i <= m; i++) {
                T[i][0] = i;
            }
            for (int j = 1; j <= n; j++) {
                T[0][j] = j;
            }
    
            int cost;
            for (int i = 1; i <= m; i++) {
                for (int j = 1; j <= n; j++) {
                    cost = X[i - 1] == Y[j - 1] ? 0: 1;
                    T[i][j] = Math.Min(Math.Min(T[i - 1][j] + 1, T[i][j - 1] + 1),
                            T[i - 1][j - 1] + cost);
                }
            }
    
            return T[m][n];
        }
    
        /// <summary>find similarity of two strings</summary>
        /// <param name="x">string x</param>
        /// <param name="y">string y</param>
        public static float FindSimilarity(string x, string y) {
            if (x == null || y == null) return 0;
    
            float maxLength = Math.Max(x.Length, y.Length);
            if (maxLength > 0) {
                // optionally ignore case if needed
                return (maxLength - GetEditDistance(x, y)) / maxLength;
            }
            return 1.0f;
        }

        /// <summary>get time information of [HH:mm:ss]</summary>
        public static string TimeInfo{
            get{
                var time = DateTime.Now;
                return $"[{PadZero(time.Hour)}:{PadZero(time.Minute)}:{PadZero(time.Second)}] ";
            }
        }
        static string PadZero(int value){
            return value < 10 ? $"0{value}" : $"{value}";
        }
    }


}