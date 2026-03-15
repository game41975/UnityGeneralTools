using UnityEditor;
using UnityEngine;

namespace DataImporter
{ 
    public class MenuWindow : EditorWindow
    {
        /// <summary>出力するScriptableObjectに設定するスクリプト名</summary>
        private string scriptName = string.Empty;
        private string excelFilePath = string.Empty;
        private string exportSOPath = string.Empty;

        [MenuItem("DataImporter/OpenMenuWindow")]
        private static void OpenWindow()
        {
            var window = GetWindow<MenuWindow>("UIElements");
            window.titleContent = new GUIContent("DataImportMenu");
            window.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("読み込むエクセルファイルを選択"))
            {
                excelFilePath = EditorUtility.OpenFilePanel("SelectFile", "", "xlsx,xlsm");
            }
            GUI.enabled = false;
            EditorGUILayout.TextField("エクセルファイルパス", excelFilePath);
            GUI.enabled = true;

            GUILayout.Space(20);

            scriptName = EditorGUILayout.TextField("スクリプトファイル名", scriptName);

            GUILayout.Space(20);

            if (GUILayout.Button("ScriptableObjectの出力先を選択(未入力でAssets以下に出力)"))
            {
                exportSOPath = EditorUtility.OpenFolderPanel("SelectExportFolder", "", "");
                if (!string.IsNullOrEmpty(exportSOPath))
                {
                    exportSOPath = DataImportManager.ConvertAbsolutePathToAssetsPath(exportSOPath);
                }
            }
            GUI.enabled = false;
            EditorGUILayout.TextField("出力先", exportSOPath);
            GUI.enabled = true;

            GUILayout.Space(20);

            if (GUILayout.Button("データ出力開始"))
            {
                if (string.IsNullOrEmpty(excelFilePath))
                {
                    Debug.LogError("読み込み対象のファイルが選択されていません");
                    return;
                }

                if (string.IsNullOrEmpty(scriptName))
                {
                    Debug.LogError("出力するスクリプトの名前を入力してください");
                    return;
                }

                //if(string.IsNullOrEmpty(exportSOPath))
                //{
                //    Debug.LogError("ScriptableObjectの出力先を選択してください");
                //    return;
                //}

                DataImportManager.Import(excelFilePath, scriptName, exportSOPath);
            }
        }
    }
}