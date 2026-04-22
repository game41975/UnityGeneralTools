using JetBrains.Annotations;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;

namespace DataImporter
{
    public class DataImportManager
    {
        /// <summary>スクリプトファイルの出力先</summary>
        static string ExportCSFilePath = $"{Application.dataPath}/Scripts/DataImporter";
        /// <summary></summary>
        static string ExportCSVFilePath = $"UnityGeneralTools/DataImporter/CSV";

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

            StringBuilder csv = new StringBuilder();
            StringBuilder csvRow = new StringBuilder();

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
                        if (rowNum > 2)
                        {
                            csv.AppendLine(csvRow.ToString().TrimEnd(','));
                            csvRow.Clear();
                        }

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

                    string csvValue = value;
                    if (csvValue.Contains(",") || csvValue.Contains("\"") || csvValue.Contains("\n")) 
                    {
                        csvValue = "\"" + csvValue.Replace("\"", "\"\"") + "\"";
                    }
                    csvRow.Append(csvValue + ",");
                }
                columnNum++;//次の列へ
            }

            //ExportCSVFile($"{Application.dataPath}/{ExportCSVFilePath}", scriptName, csv.ToString());
            ExportSOScriptFile(paramItemList, paramTypeList, fileName, ExportCSFilePath, scriptName);

            int dataRowCount = dataList.Count;
            int dataColumnCount = 0;
            if(dataList.Count > 0)
            {
                dataColumnCount = dataList[0].Count;
            }

            TempExportData.instance.dataArray = new string[dataRowCount * dataColumnCount];
            for(int i = 0; i < dataList.Count; i++)
            {
                for(int j = 0;j<dataList[i].Count; j++)
                {
                    TempExportData.instance.dataArray[i * dataColumnCount + j] = dataList[i][j];
                }
            }

            TempExportData.instance.soClassName = $"{scriptName}List";
            TempExportData.instance.objectName = scriptName;
            TempExportData.instance.exportPath = exportFolderPath;
            TempExportData.instance.dataRowCount = dataRowCount;
            TempExportData.instance.dataColumnCount = dataColumnCount;
        }

        /// <summary>
        /// 指定のパラメータを持つScriptableObjectクラスのスクリプトファイルの生成
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="typeList"></param>
        private static void ExportSOScriptFile(List<string> itemList, List<string> typeList, string scriptableObjectName, string exportPath, string csvFileName)
        {
            string outPutCSFilePath = $"{exportPath}/{scriptableObjectName}List.cs";
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            string csvFilePath = $"{ExportCSVFilePath}/{csvFileName}.csv";

            using (var sw = new StreamWriter(outPutCSFilePath,false))
            {
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using System.Reflection;");
                sw.WriteLine("using UnityEngine;\n");
                sw.WriteLine("//******************************");
                sw.WriteLine("// Output by DataImporter.cs");
                sw.WriteLine("//******************************\n");
                sw.WriteLine("namespace DataImporter");
                sw.WriteLine("{");
                sw.WriteLine($"    [CreateAssetMenu(fileName =\"{scriptableObjectName}List\",menuName = \"DataImporter/CreateScriptableObject/{scriptableObjectName}\")]");
                sw.WriteLine($"    public class {scriptableObjectName}List : ScriptableObjectBase");
                sw.WriteLine("    {");
                //sw.WriteLine($"        private readonly string CSVFilePath = $\"{{Application.dataPath}}/{csvFilePath}\";\n");
                sw.WriteLine("        [SerializeField]");
                sw.WriteLine($"        public List<{scriptableObjectName}> m_dataList = new List<{scriptableObjectName}>();\n");

                //sw.WriteLine("        public void OnEnable()");
                //sw.WriteLine("        {");
                //sw.WriteLine("            var setData = DataImporter.DataImportSupport.LoadCsv(CSVFilePath);");
                //sw.WriteLine("            InitParam(setData);");
                //sw.WriteLine("        }\n");

                sw.WriteLine("        public override void InitParam(List<List<string>> list)");
                sw.WriteLine("        {");
                sw.WriteLine("            if(list == null || list[0] == null)\r\n            {\r\n                return;\r\n            }\n");
                sw.WriteLine("            m_dataList.Clear();");
                sw.WriteLine($"            FieldInfo[] fields = typeof({scriptableObjectName}).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);\r\n");
                
                sw.WriteLine("            const int headerCount = 2;");
                sw.WriteLine("            for(int row = 0; row < list.Count; row++)");
                sw.WriteLine("            {");
                sw.WriteLine($"                if (list[row].Count != fields.Length)\r\n                {{\r\n                    Debug.LogError($\"The number of elements does not match: row:{{row + headerCount}}\");\r\n                    continue;\r\n                }}");
                //sw.WriteLine("            for(int colmun = 0; colmun < list[row].Count; colmun++)");

                //sw.WriteLine("            {");
                sw.WriteLine($"                var addParam = new {scriptableObjectName}();");
                for (int i = 0; i < itemList.Count; i++)
                {
                    sw.WriteLine($"                DataImportSupport.ConvertParam<{typeList[i]}>(list[row][{i}], (_convertParam) =>{{ addParam.{itemList[i]} = _convertParam; }});");
                }
                sw.WriteLine("                m_dataList.Add(addParam);");
                //sw.WriteLine("            }");
                sw.WriteLine("            }");
                sw.WriteLine("        }\n");

                sw.WriteLine("        public override void InitParam(int rowCount, int columnCount, string[] dataArray)");
                sw.WriteLine("        {");
                sw.WriteLine("            if(dataArray == null || dataArray.Length < rowCount * columnCount)\r\n            {\r\n                return;\r\n            }\n");
                sw.WriteLine("            m_dataList.Clear();\n");
                
                sw.WriteLine("            for(int i = 0; i < rowCount; i++)");
                sw.WriteLine("            {");
                sw.WriteLine($"                var addParam = new {scriptableObjectName}();");
                for (int i = 0; i < itemList.Count; i++)
                {
                    sw.WriteLine($"                DataImportSupport.ConvertParam<{typeList[i]}>(dataArray[i * columnCount + {i}], (_convertParam) =>{{ addParam.{itemList[i]} = _convertParam; }});");
                }
                sw.WriteLine("                m_dataList.Add(addParam);");
                sw.WriteLine("            }");

                sw.WriteLine("        }");


                sw.WriteLine("    }\n");

                sw.WriteLine("    [System.Serializable]");
                sw.WriteLine($"    public class {scriptableObjectName} : ScriptableObjectParameterBase");
                sw.WriteLine("    {");

                for (int i = 0; i < itemList.Count; i++)
                {
                    sw.WriteLine("        [SerializeField]");
                    sw.WriteLine($"        public {typeList[i]} {itemList[i]};");
                }
                sw.WriteLine("    }\n");

                sw.WriteLine("}\n");
            }

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        private static void ExportCSVFile(string filePath, string fileName, string data)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            File.WriteAllText($"{filePath}/{fileName}.csv", data, Encoding.UTF8);
        }

        /// <summary>
        /// ScriptableObject?o??
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="dataList"></param>
        private static void ExportSO(string scriptName, string soName, int rowCount,int columnCount, string[] dataArray, string exportPath = null)
        {
            var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
            if (assembly == null)
            {
                Debug.LogError("Assembly load failure");
                return;
            }

            Type type = assembly.GetType($"DataImporter.{scriptName}");
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
                                $"{exportPath}" :
                                $"Assets";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string assetPath = $"{folderPath}/{soName}.asset";

            AssetDatabase.CreateAsset(obj, assetPath);
            obj.InitParam(rowCount, columnCount, dataArray);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();

            Debug.Log($"CreateScriptableObject");
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

        [DidReloadScripts]
        private static void CheckExportSO()
        {
            //ScriptableObject生成に必要な情報がそろっているかチェック
            if (!string.IsNullOrEmpty(TempExportData.instance.soClassName) &&
                !string.IsNullOrEmpty(TempExportData.instance.objectName) 
                /*&& !string.IsNullOrEmpty(TempExportData.instance.exportPath)*/)
            {
                ExportSO(TempExportData.instance.soClassName,
                         TempExportData.instance.objectName,
                         TempExportData.instance.dataRowCount,
                         TempExportData.instance.dataColumnCount,
                         TempExportData.instance.dataArray,
                         TempExportData.instance.exportPath);
                
                TempExportData.instance.InitParams();
            }
        } 
    }

    public class TempExportData : ScriptableSingleton<TempExportData>
    {
        public string soClassName;
        public string objectName;
        public string exportPath;

        public int dataRowCount;
        public int dataColumnCount;
        public string[] dataArray;

        /// <summary>
        /// 情報初期化
        /// </summary>
        public void InitParams()
        {
            soClassName = string.Empty;
            objectName = string.Empty;
            exportPath = string.Empty;
            dataRowCount = 0;
            dataColumnCount = 0;
            dataArray = null;
        }
    }
}
