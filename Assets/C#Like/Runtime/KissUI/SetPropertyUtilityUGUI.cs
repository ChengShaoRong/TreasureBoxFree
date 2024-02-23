/*
 * This file duplicate from "com.unity.ugui@1.0.0\Runtime\UI\Core\SetPropertyUtility.cs", copyright owned by Unity Technologies.
 * Why has to duplicate? Because it's a internal class.
 * Changed log (By RongRong):
 *  1 Class name : SetPropertyUtility -> SetPropertyUtilityUGUI
 * 
 * ���ļ�������"com.unity.ugui@1.0.0\Runtime\UI\Core\SetPropertyUtility.cs",��Ȩ��Unity����.
 * Ϊ��Ҫ���Ƴ���?��Ϊ���趨Ϊ�ڲ���.
 * �޸�����(�������޸�):
 *  1 ���� : SetPropertyUtility -> SetPropertyUtilityUGUI
 */
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    internal static class SetPropertyUtilityUGUI
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
