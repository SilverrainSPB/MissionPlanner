using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Utils
{
    public class MiscUtils
    {
        public static T ParseEnum<T>(string name) {
            return (T)Enum.Parse(typeof(T), name);
        }

        public static void Swap<T>(ref T a, ref T b) {
            T tmp = a;
            a = b;
            b = tmp;
        }
        
        public static void Swap<T1, T2>(ref T1 a, ref T2 b, Dictionary<string, string> paths)
        {
            foreach (var pair in paths)
            {
                string[] leftPath = pair.Key.Split(new char[] { '-' });
                string[] rightPath = pair.Value.Split(new char[] { '-' });

                object aParentPropertyValue = a;
                object bParentPropertyValue = b;
                for (int i = 1; i < leftPath.Length - 1; i++)
                {
                    aParentPropertyValue = aParentPropertyValue.GetType().GetProperty(leftPath[i].Trim(new char[] { '«', '»' })).GetValue(aParentPropertyValue);
                }
                PropertyInfo aPropertyInfo = aParentPropertyValue.GetType().GetProperty(leftPath[leftPath.Length - 1].Trim(new char[] { '«', '»' }));

                for (int i = 1; i < rightPath.Length - 1; i++)
                {
                    bParentPropertyValue = bParentPropertyValue.GetType().GetProperty(rightPath[i].Trim(new char[] { '«', '»' })).GetValue(bParentPropertyValue);
                }
                PropertyInfo bPropertyInfo = bParentPropertyValue.GetType().GetProperty(rightPath[rightPath.Length - 1].Trim(new char[] { '«', '»' }));

                object aTempPropertyValue = aPropertyInfo.GetValue(aParentPropertyValue);
                object bTempPropertyValue = bPropertyInfo.GetValue(bParentPropertyValue);

                Type aPropertyType = aPropertyInfo.PropertyType;
                Type bPropertyType = bPropertyInfo.PropertyType;
                MethodInfo aExpliciteMethod = aPropertyType.GetMethod(
                                "op_Explicit",
                                (BindingFlags.Public | BindingFlags.Static),
                                null,
                                new Type[] { bPropertyType },
                                new ParameterModifier[0]
                            );

                MethodInfo bExpliciteMethod = bPropertyType.GetMethod(
                                    "op_Explicit",
                                    (BindingFlags.Public | BindingFlags.Static),
                                    null,
                                    new Type[] { aPropertyType },
                                    new ParameterModifier[0]
                                );
                if (aExpliciteMethod != null && bExpliciteMethod != null)
                {
                    aPropertyInfo.SetValue(aParentPropertyValue, aExpliciteMethod.Invoke(aTempPropertyValue, new object[] { bTempPropertyValue }));
                    bPropertyInfo.SetValue(bParentPropertyValue, bExpliciteMethod.Invoke(bTempPropertyValue, new object[] { aTempPropertyValue }));
                }
                else if (aPropertyType.IsValueType)
                {
                    if (aPropertyType == bPropertyType)
                    {
                        aPropertyInfo.SetValue(aParentPropertyValue, bTempPropertyValue);
                        bPropertyInfo.SetValue(bParentPropertyValue, aTempPropertyValue);
                    }
                    else
                    {
                        try
                        {
                            aPropertyInfo.SetValue(aParentPropertyValue, Convert.ChangeType(bTempPropertyValue, aPropertyType));
                            bPropertyInfo.SetValue(bParentPropertyValue, Convert.ChangeType(aTempPropertyValue, bPropertyType));
                        }
                        catch
                        {
                            throw new Exception($"The {pair.Key} and {pair.Value} properties are incompatible");
                        }
                    }
                }
                else if (aPropertyType.IsClass || aPropertyType.IsInterface)
                {
                    if (aTempPropertyValue == null || bTempPropertyValue == null)
                    {
                        aPropertyInfo.SetValue(aParentPropertyValue, bTempPropertyValue);
                        bPropertyInfo.SetValue(bParentPropertyValue, aTempPropertyValue);
                    }
                    else if (aPropertyType.IsArray)
                    {
                        Type aElementType = aPropertyType.GetElementType();
                        Type bElementType = bPropertyType.GetElementType();
                        int aRank = aPropertyType.GetArrayRank();
                        int bRank = bPropertyType.GetArrayRank();
                        if (aRank != bRank)
                        {
                            throw new Exception($"Arrays {pair.Key} and {pair.Value} have a different ranks");
                        }
                        if (aElementType != bElementType)
                            throw new Exception($"Arrays {pair.Key} and {pair.Value} have a different element types");
                        int[] bLengths = new int[bRank];
                        int[] aLengths = new int[aRank];
                        int aTotalLength = (int)aPropertyType.GetProperty("Length").GetValue(aTempPropertyValue);
                        int bTotalLength = (int)bPropertyType.GetProperty("Length").GetValue(bTempPropertyValue);
                        for (int dimension = 0; dimension < bRank; dimension++)
                        {
                            bLengths[dimension] = ((Array)bTempPropertyValue).GetUpperBound(dimension) + 1;
                        }

                        for (int dimension = 0; dimension < aRank; dimension++)
                        {
                            aLengths[dimension] = ((Array)aTempPropertyValue).GetUpperBound(dimension) + 1;
                        }
                        Array NewArrayProperty = Array.CreateInstance(aElementType, bLengths);
                        Array.Copy(bTempPropertyValue as Array, NewArrayProperty, bTotalLength);
                        aPropertyInfo.SetValue(aParentPropertyValue, NewArrayProperty);

                        NewArrayProperty = Array.CreateInstance(bElementType, aLengths);
                        Array.Copy(aTempPropertyValue as Array, NewArrayProperty, aTotalLength);
                        bPropertyInfo.SetValue(bParentPropertyValue, NewArrayProperty);
                    }
                    else if (aPropertyType.IsGenericType)
                    {
                        aPropertyInfo.SetValue(aParentPropertyValue, bTempPropertyValue);
                        bPropertyInfo.SetValue(bParentPropertyValue, aTempPropertyValue);
                    }
                    else
                    {
                        Dictionary<string, string> newPaths = new Dictionary<string, string>();
                        PropertyInfo[] aPropertyInfos = aPropertyType.GetProperties();
                        foreach (var prop in aPropertyInfos)
                        {
                            if (bPropertyType.GetProperty(prop.Name) != null)
                            {
                                newPaths.Add(prop.Name, prop.Name);
                            }
                        }
                        Swap(ref aTempPropertyValue, ref bTempPropertyValue, newPaths);
                        aPropertyInfo.SetValue(aParentPropertyValue, aTempPropertyValue);
                        bPropertyInfo.SetValue(bParentPropertyValue, bTempPropertyValue);
                    }
                }
            }
        }
    }
}
