using UnityEngine;

public class HelperFunctions {
    public static void log(params System.Object[] arguments)
    {
        string finalString = string.Empty;
        for (int i = 0; i < arguments.Length; i++)
        {
            finalString += arguments[i];
            if (i != arguments.Length - 1)
                finalString += " , ";
        }
        Debug.Log(finalString);
    }
}