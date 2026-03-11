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
using static UnityEngine.Rendering.DebugUI.Table;

public class ExportData
{
    /// <summary>ScriptableObjectの出力先</summary>
    static string ExportFilePath = "RouletteBattle/Data";

    [MenuItem("RouletteBattle/Export")]
    static public void Export()
    {
        var path = EditorUtility.OpenFilePanel("SelectFile", "", "xlsx");
        if (!string.IsNullOrEmpty(path))
        {
            //Debug.Log($"FilePath:{path}");

            FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            IWorkbook book = null;
            string extension = Path.GetExtension(path);
            if(extension == ".xlsx")
            {
                book = new XSSFWorkbook(fs);
            }

            if(book != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (!Directory.Exists(ExportFilePath))
                {
                    Directory.CreateDirectory(ExportFilePath);
                }

                List<List<string>> dataList = new List<List<string>>();
                List<string> paramItemList = new List<string>();
                List<string> paramTypeList = new List<string>();

                ISheet sheet = book.GetSheetAt(0);
                int rowNum = 1;
                var row = sheet.GetRow(rowNum);
                int columnNum = 1;
                while (true)
                {
                    if (columnNum > 100) break;
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

                    if(rowNum == 1)
                    {
                        //パラメータ名
                        paramItemList.Add(value);
                    }
                    else if(rowNum == 2)
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

                ExportCSFile(paramItemList, paramTypeList);

                Test1("TestList", dataList);
            }
        }
    }

    static public void Test1(string soName, List<List<string>> dataList)
    {
        var assembly = Assembly.Load("Assembly-CSharp");
        if(assembly == null)
        {
            Debug.LogError("Assembly load failure");
            return;
        }

        Type type = assembly.GetType(soName);
        if(type == null)
        {
            Debug.LogError("The specified scriptable object was not found");
            return;
        }

        var obj = Activator.CreateInstance(type) as ScriptableObjectBase;
        if(obj == null)
        {
            Debug.LogError("Scriptable object creation failed");
            return;
        }

        obj.InitParam(dataList);
        AssetDatabase.CreateAsset(obj, $"Assets/{ExportFilePath}/Test.asset");
        AssetDatabase.Refresh();
        //Debug.Log("");
    }

    /// <summary>
    /// 指定のパラメータを持つScriptableObjectクラスの生成
    /// </summary>
    /// <param name="itemList"></param>
    /// <param name="typeList"></param>
    private static void ExportCSFile(List<string> itemList, List<string> typeList)
    {
        string scriptableObjectName = "Test";
        string outPutCSFilePath = $"{Application.dataPath}/Scripts/RouletteBattle/Data/{scriptableObjectName}.cs";

        using (var sw = new StreamWriter(outPutCSFilePath))
        {
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine("using System.Reflection;");
            sw.WriteLine("using UnityEngine;\n");
            sw.WriteLine("//******************************");
            sw.WriteLine("// Output by ExportData.cs");
            sw.WriteLine("//******************************\n");
            //sw.WriteLine($"[CreateAssetMenu(fileName =\"{scriptableObjectName}List\",menuName = \"ScriptableObject/{scriptableObjectName}List\")]");
            sw.WriteLine($"public class {scriptableObjectName}List : ScriptableObjectBase");
            sw.WriteLine("{");
            sw.WriteLine("    [SerializeField]");
            sw.WriteLine($"    public List<{scriptableObjectName}> m_dataList = new List<{scriptableObjectName}>();\n");

            sw.WriteLine("    public override void InitParam(List<List<string>> list)");
            sw.WriteLine("    {");
            sw.WriteLine("        if(list == null || list[0] == null)\r\n        {\r\n            return;\r\n        }\n");

            sw.WriteLine($"        FieldInfo[] fields = typeof({scriptableObjectName}).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);\r\n\n");
            sw.WriteLine("        const int headerCount = 2;");
            sw.WriteLine("        for(int row = 0; row < list.Count; row++)");
            sw.WriteLine("        {");
            sw.WriteLine($"            if (list[row].Count != fields.Length)\r\n            {{\r\n                Debug.LogError($\"The number of elements does not match: row:{{row + headerCount}}\");\r\n                continue;\r\n            }}");
            //sw.WriteLine("            for(int colmun = 0; colmun < list[row].Count; colmun++)");
            
            //sw.WriteLine("            {");
            sw.WriteLine($"            var addParam = new {scriptableObjectName}();");
            for (int i = 0; i < itemList.Count; i++)
            {
                sw.WriteLine($"            ConvertParam<{typeList[i]}>(list[row][{i}], (_convertParam) =>{{ addParam.{itemList[i]} = _convertParam; }});");
            }
            sw.WriteLine("            m_dataList.Add(addParam);");
            //sw.WriteLine("            }");
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
}
