using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public class HierarchyExporter
{
    // ========== НАСТРОЙКИ ==========
    private static class Settings
    {
        public static bool exportTransformValues = false;
        public static bool exportComponentProperties = false;
        public static bool exportScriptFields = false;
        public static bool exportPrivateFields = false;
        public static int maxStringLength = 2000000;
        public static bool exportReferences = true;
        public static bool exportInactiveObjects = true; // ✅ Экспортировать неактивные объекты
        public static string indentStyle = "tree"; // "tree" | "spaces" | "tabs"
    }

    // ========== МЕНЮ ==========
    [MenuItem("Tools/Export Hierarchy/Full Scene to Text")]
    public static void ExportFullScene() => ExportHierarchy(null, false);

    [MenuItem("Tools/Export Hierarchy/Selected GameObject to Text")]
    public static void ExportSelected()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("Warning", "Выделите объект в иерархии!", "OK");
            return;
        }
        ExportHierarchy(Selection.activeGameObject.transform, true);
    }

    [MenuItem("Assets/Export Prefab Hierarchy to Text")]
    public static void ExportPrefabFromProject()
    {
        var selected = Selection.activeObject;
        if (selected == null || !PrefabUtility.IsPartOfPrefabAsset(selected))
        {
            EditorUtility.DisplayDialog("Warning", "Выделите префаб в окне Project!", "OK");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(selected));
        if (prefabRoot != null)
        {
            ExportHierarchy(prefabRoot.transform, true, prefabRoot.name + "_hierarchy");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    // ========== ОСНОВНОЙ ЭКСПОРТ ==========
    private static void ExportHierarchy(Transform root, bool selectedOnly, string defaultFileName = "hierarchy")
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Hierarchy Export ===");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine($"Root: {(root != null ? root.name : "Scene Root")}");
        sb.AppendLine($"Settings: Transform={Settings.exportTransformValues}, Components={Settings.exportComponentProperties}, Scripts={Settings.exportScriptFields}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine("Формат: [A] {Layer} <Tag> (Static) -- Name (ID)");
        sb.AppendLine("A: V=Active+Visible, X=Inactive, H=Hidden");
        sb.AppendLine(new string('=', 80));

        if (root != null)
        {
            AppendTransform(root, 0, sb, selectedOnly);
        }
        else
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootObjects)
                AppendTransform(go.transform, 0, sb, selectedOnly);
        }

        string path = EditorUtility.SaveFilePanel("Save Hierarchy", "", defaultFileName, "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"✅ Иерархия сохранена: {path}");
        }
    }

    // ========== ГЕНЕРАЦИЯ ОТСТУПА ==========
    private static string GetIndent(int depth)
    {
        switch (Settings.indentStyle)
        {
            case "tabs":
                return new string('\t', depth);
            case "spaces":
                return new string(' ', depth * 2);
            case "tree":
            default:
                if (depth == 0) return "";
                string result = "";
                for (int i = 0; i < depth - 1; i++)
                    result += "│   ";
                result += "├─ ";
                return result;
        }
    }

    private static string GetChildIndent(int depth)
    {
        // Отступ для свойств внутри объекта (не для дочерних объектов)
        switch (Settings.indentStyle)
        {
            case "tabs":
                return new string('\t', depth) + "  ";
            case "spaces":
                return new string(' ', depth * 2 + 2);
            case "tree":
            default:
                if (depth == 0) return "  ";
                string result = "";
                for (int i = 0; i < depth; i++)
                    result += "│   ";
                result += "   ";
                return result;
        }
    }

    // ========== РЕКУРСИВНЫЙ ОБХОД ==========
    private static void AppendTransform(Transform t, int depth, StringBuilder sb, bool selectedOnly)
    {
        // ✅ ИСПРАВЛЕНО: проверяем настройку exportInactiveObjects
        if (selectedOnly && !Settings.exportInactiveObjects && !t.gameObject.activeInHierarchy)
            return;

        string objIndent = GetIndent(depth);
        string propIndent = GetChildIndent(depth);

        string status = !t.gameObject.activeSelf ? "X" : (t.gameObject.activeInHierarchy ? "V" : "H");
        string layer = LayerMask.LayerToName(t.gameObject.layer);
        string tag = t.gameObject.tag;
        bool isStatic = t.gameObject.isStatic;
        int instanceID = t.GetInstanceID();

        // === Заголовок объекта ===
        sb.Append($"{objIndent}[{status}] {{{layer}}} <{tag}>");
        if (isStatic) sb.Append(" (Static)");
        sb.Append($" -- {t.name} (ID:{instanceID})");

        // === Префаб-информация ===
        var prefabType = PrefabUtility.GetPrefabInstanceStatus(t.gameObject);
        if (prefabType == PrefabInstanceStatus.Connected)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (source != null)
                sb.Append($" ⎈[{Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source))}]");
        }
        else if (prefabType == PrefabInstanceStatus.MissingAsset)
            sb.Append($" ⎈[Model]");

        sb.AppendLine();

        // === Детали компонентов ===
        var components = t.GetComponents<Component>();

        foreach (var comp in components)
        {
            if (comp == null) continue;

            // Transform - обрабатываем отдельно
            if (comp is Transform transform)
            {
                if (Settings.exportTransformValues)
                {
                    sb.AppendLine($"{propIndent}▶ Transform:");
                    sb.AppendLine($"{propIndent}   ├─ position: {FormatVector3(transform.localPosition)}");
                    sb.AppendLine($"{propIndent}   ├─ rotation: {FormatVector3(transform.localEulerAngles)}°");
                    sb.AppendLine($"{propIndent}   ├─ scale: {FormatVector3(transform.localScale)}");
                    sb.AppendLine($"{propIndent}   └─ eulerAngles: {FormatVector3(transform.eulerAngles)}°");
                }
                continue;
            }

            // Заголовок компонента
            string compName = comp.GetType().Name;
            if (comp is MonoBehaviour) compName = $"*{compName}*";
            sb.AppendLine($"{propIndent}{compName}");

            string compPropIndent = propIndent + "   ";

            // Экспорт свойств компонента через SerializedObject
            if (Settings.exportComponentProperties)
                ExportComponentProperties(comp, sb, compPropIndent);

            // Экспорт полей скриптов через рефлексию (только для MonoBehaviour)
            if (Settings.exportScriptFields && comp is MonoBehaviour mb)
            {
                ExportScriptFieldsViaReflection(mb, sb, compPropIndent);
            }
        }

        // ✅ ✅ ✅ ИСПРАВЛЕНО: РЕКУРСИЯ ПО ДЕТЯМ (БЫЛА ПРОПУЩЕНА!) ✅ ✅ ✅
        foreach (Transform child in t)
        {
            AppendTransform(child, depth + 1, sb, selectedOnly);
        }
    }

    // ========== ЭКСПОРТ СВОЙСТВ КОМПОНЕНТА ==========
    private static void ExportComponentProperties(Component comp, StringBuilder sb, string indent)
    {
        try
        {
            var serializedObj = new SerializedObject(comp);
            var prop = serializedObj.GetIterator();
            bool enterChildren = true;

            while (prop.Next(enterChildren))
            {
                enterChildren = false;

                if (IsSkippedProperty(prop.name)) continue;

                string value = FormatPropertyValue(prop);
                if (!string.IsNullOrEmpty(value))
                    sb.AppendLine($"{indent}• {prop.name}: {value}");
            }
        }
        catch (System.Exception ex)
        {
            sb.AppendLine($"{indent}⚠ Error reading properties: {ex.Message}");
        }
        // ✅ Убран дублирующий вызов ExportScriptFieldsViaReflection отсюда
    }

    // ========== ФИЛЬТР СЛУЖЕБНЫХ ПОЛЕЙ ==========
    private static bool IsSkippedProperty(string name)
    {
        string[] skipped = { "m_Script", "m_ObjectHideFlags", "m_PrefabInstance",
                           "m_PrefabAsset", "m_GameObject", "m_Component",
                           "script", "enabled", "m_Enabled", "m_EditorHideFlags" };
        foreach (var s in skipped)
            if (name.Equals(s, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // ========== ФОРМАТИРОВАНИЕ ЗНАЧЕНИЯ СВОЙСТВА ==========
    private static string FormatPropertyValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                return prop.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return prop.boolValue ? "✓ true" : "✗ false";
            case SerializedPropertyType.Float:
                return prop.floatValue.ToString("F3");
            case SerializedPropertyType.String:
                return $"\"{Truncate(prop.stringValue)}\"";
            case SerializedPropertyType.Color:
                var c = prop.colorValue;
                return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
            case SerializedPropertyType.Vector2:
                var v2 = prop.vector2Value;
                return $"({v2.x:F2}, {v2.y:F2})";
            case SerializedPropertyType.Vector3:
                var v3 = prop.vector3Value;
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            case SerializedPropertyType.Vector4:
                var v4 = prop.vector4Value;
                return $"({v4.x:F2}, {v4.y:F2}, {v4.z:F2}, {v4.w:F2})";
            case SerializedPropertyType.Rect:
                var r = prop.rectValue;
                return $"Rect(x:{r.x:F1}, y:{r.y:F1}, w:{r.width:F1}, h:{r.height:F1})";
            case SerializedPropertyType.Bounds:
                var b = prop.boundsValue;
                return $"Bounds(center:{FormatVector3(b.center)}, size:{FormatVector3(b.size)})";
            case SerializedPropertyType.Quaternion:
                var q = prop.quaternionValue;
                return $"({q.x:F3}, {q.y:F3}, {q.z:F3}, {q.w:F3})";
            case SerializedPropertyType.Enum:
                return prop.enumDisplayNames[prop.enumValueIndex];
            case SerializedPropertyType.ObjectReference:
                var obj = prop.objectReferenceValue;
                if (Settings.exportReferences && obj != null)
                    return $"[{obj.GetType().Name}] \"{obj.name}\"";
                return obj == null ? "null" : "[reference]";
            case SerializedPropertyType.LayerMask:
                return $"LayerMask({prop.intValue})";
            case SerializedPropertyType.AnimationCurve:
                var curve = prop.animationCurveValue;
                return curve != null ? $"[Curve: {curve.keys.Length} keys]" : "[Curve: null]";
            case SerializedPropertyType.Gradient:
                return "[Gradient]";
            case SerializedPropertyType.ArraySize:
                return $"[Array size: {prop.arraySize}]";
            default:
                return $"[{prop.propertyType}]";
        }
    }

    // ========== ЭКСПОРТ ПОЛЕЙ СКРИПТОВ ЧЕРЕЗ РЕФЛЕКСИЮ ==========
    private static void ExportScriptFieldsViaReflection(MonoBehaviour mb, StringBuilder sb, string indent)
    {
        var type = mb.GetType();
        var flags = BindingFlags.Public | BindingFlags.Instance;

        if (Settings.exportPrivateFields)
            flags |= BindingFlags.NonPublic;

        var fields = type.GetFields(flags);

        foreach (var field in fields)
        {
            if (field.Name.StartsWith("m_") && !Settings.exportPrivateFields) continue;
            if (field.IsDefined(typeof(System.ObsoleteAttribute), false)) continue;
            if (field.IsStatic || field.IsLiteral) continue;

            try
            {
                object value = field.GetValue(mb);
                string formatted = FormatReflectionValue(value);
                if (!string.IsNullOrEmpty(formatted) && !formatted.StartsWith("["))
                    sb.AppendLine($"{indent}◦ {field.Name}: {formatted}");
            }
            catch { /* Пропускаем поля, которые нельзя прочитать */ }
        }
    }

    // ========== ФОРМАТИРОВАНИЕ ЗНАЧЕНИЙ ИЗ РЕФЛЕКСИИ ==========
    private static string FormatReflectionValue(object value)
    {
        if (value == null) return "null";

        if (value is bool b) return b ? "✓ true" : "✗ false";
        if (value is int i) return i.ToString();
        if (value is float f) return f.ToString("F3");
        if (value is double d) return d.ToString("F3");
        if (value is string s) return $"\"{Truncate(s)}\"";

        if (value is Vector3 v3) return FormatVector3(v3);
        if (value is Vector2 v2) return $"({v2.x:F2}, {v2.y:F2})";
        if (value is Vector4 v4) return $"({v4.x:F2}, {v4.y:F2}, {v4.z:F2}, {v4.w:F2})";
        if (value is Color col) return $"RGBA({col.r:F2}, {col.g:F2}, {col.b:F2}, {col.a:F2})";
        if (value is Quaternion q) return $"Euler({q.eulerAngles.x:F1}°, {q.eulerAngles.y:F1}°, {q.eulerAngles.z:F1}°)";
        if (value is Rect rect) return $"Rect({rect.x:F1}, {rect.y:F1}, {rect.width:F1}x{rect.height:F1})";

        if (value is UnityEngine.Object obj)
            return Settings.exportReferences ? $"[{obj.GetType().Name}] \"{obj.name}\"" : "[reference]";

        if (value is System.Collections.IList list)
            return $"[List: {list.Count} items]";

        if (value is System.Collections.IDictionary dict)
            return $"[Dict: {dict.Count} entries]";

        if (value is System.Enum e) return e.ToString();

        return $"[{value.GetType().Name}]";
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
    private static string FormatVector3(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

    private static string Truncate(string s, int? maxLength = null)
    {
        int limit = maxLength ?? Settings.maxStringLength;
        if (string.IsNullOrEmpty(s) || s.Length <= limit) return s;
        return s.Substring(0, limit - 3) + "...";
    }

    // ========== КОПИРОВАНИЕ В БУФЕР ==========
    [MenuItem("Tools/Export Hierarchy/Copy Selected to Clipboard")]
    public static void CopySelectedToClipboard()
    {
        if (Selection.activeGameObject == null) return;

        StringBuilder sb = new StringBuilder();
        AppendTransform(Selection.activeGameObject.transform, 0, sb, false);

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("📋 Иерархия скопирована в буфер обмена!");
    }

    // ========== НАСТРОЙКИ ЧЕРЕЗ МЕНЮ ==========
    [MenuItem("Tools/Export Hierarchy/Settings")]
    public static void OpenSettings()
    {
        string msg = "🔧 Настройки экспорта:\n\n";
        msg += $"• Transform values: {Settings.exportTransformValues}\n";
        msg += $"• Component properties: {Settings.exportComponentProperties}\n";
        msg += $"• Script fields: {Settings.exportScriptFields}\n";
        msg += $"• Private fields: {Settings.exportPrivateFields}\n";
        msg += $"• Export references: {Settings.exportReferences}\n";
        msg += $"• Export inactive objects: {Settings.exportInactiveObjects}\n";
        msg += $"• Indent style: {Settings.indentStyle}\n\n";
        msg += "Чтобы изменить настройки, отредактируйте статические поля в классе Settings внутри скрипта.";

        EditorUtility.DisplayDialog("Hierarchy Exporter Settings", msg, "OK");
    }
}