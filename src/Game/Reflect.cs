using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NORevokerECR.Game
{
    // Reads and writes the game's private serialized fields by name. Fields are
    // looked up along the base types, so TargetDetector fields work on a Radar.
    internal static class Reflect
    {
        internal static T Get<T>(object target, string field)
        {
            FieldInfo f = Field(target.GetType(), field);
            return (T)f.GetValue(target);
        }

        internal static void Set(object target, string field, object value)
        {
            Field(target.GetType(), field).SetValue(target, value);
        }

        // Copies the named fields from source to target (same type or a common base).
        internal static void Copy(object source, object target, params string[] fields)
        {
            foreach (string name in fields)
                Set(target, name, Field(source.GetType(), name).GetValue(source));
        }

        // Copies every field Unity serializes (public, or [SerializeField]) from
        // one component to another of the same type, up to MonoBehaviour.
        internal static void CopySerialized(Component source, Component target)
        {
            for (Type t = source.GetType(); t != null && t != typeof(MonoBehaviour) && t != typeof(Component); t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsNotSerialized || f.IsLiteral || f.IsInitOnly) continue;
                    if (!f.IsPublic && f.GetCustomAttribute<SerializeField>() == null) continue;
                    f.SetValue(target, f.GetValue(source));
                }
            }
        }

        // "name=value, name=value" for the log; unknown fields read as "?".
        internal static string Describe(object target, params string[] fields)
        {
            return string.Join(", ", fields.Select(name =>
            {
                try
                {
                    return name + "=" + Format(Field(target.GetType(), name).GetValue(target));
                }
                catch
                {
                    return name + "=?";
                }
            }));
        }

        internal static string Format(object value)
        {
            switch (value)
            {
                case null: return "null";
                case float f: return f.ToString("0.###", CultureInfo.InvariantCulture);
                case UnityEngine.Object o: return o == null ? "null" : o.name;
                case string s: return s;
                case System.Collections.IEnumerable e:
                    return "[" + string.Join(", ", e.Cast<object>().Select(Format)) + "]";
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static readonly Dictionary<(Type, string), FieldInfo> cache = new Dictionary<(Type, string), FieldInfo>();

        private static FieldInfo Field(Type type, string name)
        {
            if (cache.TryGetValue((type, name), out FieldInfo f)) return f;
            f = AccessTools.Field(type, name) ?? throw new MissingFieldException(type.Name, name);
            cache[(type, name)] = f;
            return f;
        }
    }
}
