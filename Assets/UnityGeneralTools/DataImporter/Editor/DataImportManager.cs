using JetBrains.Annotations;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace DataImporter
{
    public class DataImportManager
    {
        /// <summary>スクリプトファイルの出力先</summary>
        static string ExportCSFilePath = $"{Application.dataPath}/Scripts/DataImporter";

        static public void Import(string importFilePath, string scriptName, string exportFolderPath)
        {
            FileStream fs = File.Open(importFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            IWorkbook book = null;
            string extension = Path.GetExtension(importFilePath);
            if (extension == ".xlsx" || extension == ".xlsm")
            {
                book = new XSSFWorkbook(fs);
            }

            if(book == null)
            {
                Debug.LogError($"Failed Load ExcelFile: path={importFilePath}");
                return;
            }

            //string fileName = Path.GetFileNameWithoutExtension(importFilePath);//エクセルのタイトル名を取得
            string fileName = scriptName;

            List<List<string>> dataList = new List<List<string>>();
            List<string> paramItemList = new List<string>();
            List<string> paramTypeList = new List<string>();

            ISheet sheet = book.GetSheetAt(0);
            int rowNum = 1;
            var row = sheet.GetRow(rowNum);
            int columnNum = 1;
            while (true)
            {
                var cell = row.GetCell(columnNum);
                if (cell == null || cell.CellType == CellType.Blank)
                {
                    if (columnNum == 1) break;
                    else
                    {
                        rowNum++;//次の行へ
                        row = sheet.GetRow(rowNum);
                        columnNum = 1;
                        continue;
                    }
                }

                string value = "";
                switch (cell.CellType)
                {
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            //日付
                            value = cell.DateCellValue.ToString();
                        }
                        else
                        {
                            //数値
                            value = cell.NumericCellValue.ToString();
                        }
                        break;
                    case CellType.String:
                        value = cell.StringCellValue;
                        break;
                }

                if (rowNum == 1)
                {
                    //パラメータ名
                    paramItemList.Add(value);
                }
                else if (rowNum == 2)
                {
                    //パラメータの型
                    paramTypeList.Add(value);
                }
                else
                {
                    //パラメータ
                    if (columnNum == 1)
                    {
                        dataList.Add(new List<string>());
                    }
                    dataList[rowNum - 3].Add(value);
                }
                columnNum++;//次の列へ
            }

            ExportCSFile(paramItemList, paramTypeList, fileName, ExportCSFilePath);

            ExportSO($"{fileName}List", dataList, exportFolderPath);

            Debug.Log("DataImport Success");
        }

        /// <summary>
        /// ScriptableObject出力
        /// </summary>
        /// <param name="soName"></param>
        /// <param name="dataList"></param>
        static public void ExportSO(string soName, List<List<string>> dataList,string exportPath)
        {
            var assembly = Assembly.Load("Assembly-CSharp");
            if (assembly == null)
            {
                Debug.LogError("Assembly load failure");
                return;
            }

            Type type = assembly.GetType($"DataImporter.{soName}");
            if (type == null)
            {
                Debug.LogError("The specified scriptable object was not found");
                return;
            }

            var obj = Activator.CreateInstance(type) as ScriptableObjectBase;
            if (obj == null)
            {
                Debug.LogError("Scriptable object creation failed");
                return;
            }

            string folderPath = !string.IsNullOrEmpty(exportPath) ?
                                $"Assets/{exportPath}" :
                                $"Assets";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string assetPath = $"{folderPath}/{soName}.asset";

            obj.InitParam(dataList);
            AssetDatabase.CreateAsset(obj, assetPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 指定のパラメータを持つScriptableObjectクラスのスクリプトファイルの生成
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="typeList"></param>
        private static void ExportCSFile(List<string> itemList, List<string> typeList, string scriptableObjectName, string exportPath)
        {
            string outPutCSFilePath = $"{exportPath}/{scriptableObjectName}.cs";
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            using (var sw = new StreamWriter(outPutCSFilePath))
            {
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using System.Reflection;");
                sw.WriteLine("using UnityEngine;\n");
                sw.WriteLine("//******************************");
                sw.WriteLine("// Output by DataImporter.cs");
                sw.WriteLine("//******************************\n");
                //sw.WriteLine($"[CreateAssetMenu(fileName =\"{scriptableObjectName}List\",menuName = \"ScriptableObject/{scriptableObjectName}List\")]");
                sw.WriteLine("namespace DataImporter");
                sw.WriteLine("{");
                sw.WriteLine($"    public class {scriptableObjectName}List : ScriptableObjectBase");
                sw.WriteLine("    {");
                sw.WriteLine("        [SerializeField]");
                sw.WriteLine($"        public List<{scriptableObjectName}> m_dataList = new List<{scriptableObjectName}>();\n");

                sw.WriteLine("        public override void InitParam(List<List<string>> list)");
                sw.WriteLine("        {");
                sw.WriteLine("            if(list == null || list[0] == null)\r\n        {\r\n            return;\r\n        }\n");

                sw.WriteLine($"            FieldInfo[] fields = typeof({scriptableObjectName}).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);\r\n\n");
                sw.WriteLine("            const int headerCount = 2;");
                sw.WriteLine("            for(int row = 0; row < list.Count; row++)");
                sw.WriteLine("            {");
                sw.WriteLine($"                if (list[row].Count != fields.Length)\r\n            {{\r\n                Debug.LogError($\"The number of elements does not match: row:{{row + headerCount}}\");\r\n                continue;\r\n            }}");
                //sw.WriteLine("            for(int colmun = 0; colmun < list[row].Count; colmun++)");

                //sw.WriteLine("            {");
                sw.WriteLine($"                var addParam = new {scriptableObjectName}();");
                for (int i = 0; i < itemList.Count; i++)
                {
                    sw.WriteLine($"                ConvertParam<{typeList[i]}>(list[row][{i}], (_convertParam) =>{{ addParam.{itemList[i]} = _convertParam; }});");
                }
                sw.WriteLine("                m_dataList.Add(addParam);");
                //sw.WriteLine("            }");
                sw.WriteLine("            }");
                sw.WriteLine("        }");

                sw.WriteLine("    }");
                sw.WriteLine("}\n");

                sw.WriteLine("[System.Serializable]");
                sw.WriteLine($"public class {scriptableObjectName}:ScriptableObjectParameterBase");
                sw.WriteLine("{");

                for (int i = 0; i < itemList.Count; i++)
                {
                    sw.WriteLine("    [SerializeField]");
                    sw.WriteLine($"    public {typeList[i]} {itemList[i]};");
                }
                sw.WriteLine("}\n");
            }

            AssetDatabase.Refresh();
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // すべての区切り文字を'/'に変換
            path = path.Replace('\\', '/');

            // 連続する'/'を単一の'/'に置換（複数回の置換で///なども対応）
            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }

            return path;
        }

        /// <summary>
        /// 絶対パスから相対パスへ変換
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAbsolutePathToAssetsPath(string windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath)) return null;

            // パスを正規化
            windowsPath = NormalizePath(windowsPath);

            // すでにAssetsから始まる場合は、正規化したパスをそのまま返す
            if (windowsPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                return windowsPath;
            }

            // Application.dataPath は "プロジェクトパス/Assets" を返す
            string projectPath = NormalizePath(Application.dataPath);
            // "Assets" フォルダまでのパスを取得
            string assetsBasePath = projectPath.Substring(0, projectPath.Length - "Assets".Length);

            // フルパスから相対パスに変換
            if (windowsPath.StartsWith(assetsBasePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return windowsPath.Substring(assetsBasePath.Length);
            }

            Debug.LogError("Invalid path: The specified path is not within the Unity project.");
            return null;
        }

        /// <summary>
        /// 相対パスから絶対パスへ変換
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertAssetsPathToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            // 入力パスを正規化
            assetPath = NormalizePath(assetPath);

            // すでにフルパスの場合は、正規化したパスをそのまま返す
            string projectPath = NormalizePath(Application.dataPath);
            string projectRoot = projectPath.Substring(0, projectPath.Length - "Assets".Length);
            if (assetPath.StartsWith(projectRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }

            // アセットパスが "Assets/" で始まっていない場合はエラー
            if (!assetPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("Invalid asset path: The path must start with 'Assets/'");
                return null;
            }

            // プロジェクトルートパスとアセットパスを結合して正規化
            return NormalizePath(Path.Combine(projectRoot, assetPath));
        }
    }

}
