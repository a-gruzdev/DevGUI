using System;
using System.Collections.Generic;

namespace DevTools
{
    internal class EnumData
    {
        public int[] Values;
        public string[] Names;
        public int AllTypeMask;
        public int AllValuesMask;
        public bool IsFlags;

        public EnumData(Type type)
        {
            IsFlags = EnumUtility.IsFlags(type);
            if (!IsFlags)
            {
                Values = EnumUtility.GetValues(type);
                Names = Enum.GetNames(type);
                return;
            }

            var values = EnumUtility.GetValues(type);
            var names = Enum.GetNames(type);
            AllTypeMask = EnumUtility.GetEnumFullMask(type);

            var hasNoneFlag = values[0] == 0;
            var hasAllFlag = values[^1] == AllTypeMask;

            var size = values.Length;
            if (!hasNoneFlag) size++;
            if (!hasAllFlag) size++;

            Values = new int[size];
            Names = new string[size];
            var startIndex = 2;

            var startOffset = 0;
            var flagsCount = values.Length;
            if (!hasNoneFlag)
                Names[0] = "None";
            else
            {
                Names[0] = names[0];
                startOffset++;
                flagsCount--;
            }
            Values[0] = 0;

            if (!hasAllFlag)
                Names[1] = "All";
            else
            {
                Names[1] = names[^1];
                flagsCount--;
            }
            AllValuesMask = (1 << flagsCount) - 1;
            Values[1] = AllValuesMask;

            for (int i = 0; i < flagsCount; i++)
            {
                var srcIndex = i + startOffset;
                var dstIndex = i + startIndex;
                Values[dstIndex] = values[srcIndex];
                Names[dstIndex] = names[srcIndex];
            }
        }
    }

    internal static class EnumUtility
    {
        private static readonly Dictionary<Type, EnumData> _enumDataCache = new();

        public static EnumData GetData(Type enumType)
        {
            if (!_enumDataCache.TryGetValue(enumType, out var data))
            {
                data = new EnumData(enumType);
                _enumDataCache[enumType] = data;
            }
            return data;
        }

        public static string GetName<T>(T value)
        {
            var data = GetData(typeof(T));
            var intValue = value.GetHashCode();
            if (intValue == 0)
                return data.Names[0];
            if (intValue == data.AllTypeMask || intValue == data.AllValuesMask)
                return data.Names[1];
            return value.ToString();
        }

        public static bool IsFlags(Type type)
        {
            foreach (var attr in type.GetCustomAttributes(false))
            {
                if (attr.GetType() == typeof(FlagsAttribute))
                    return true;
            }
            return false;
        }

        public static int GetEnumFullMask(Type enumType)
        {
            var valuesType = Enum.GetUnderlyingType(enumType);
            if (valuesType == typeof(byte)) return byte.MaxValue;
            if (valuesType == typeof(ushort)) return ushort.MaxValue;
            return -1;
        }

        public static int[] GetValues(Type enumType)
        {
            var arr = Enum.GetValues(enumType);
            var values = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                values[i] = Convert.ToInt32(arr.GetValue(i));

            return values;
        }
    }
}
