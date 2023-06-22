using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class NumericField : MonoBehaviour
{
    private InputField m_InputField;
    private bool UpdateToPrevious;

    public enum NumericTypes
    {
        PortNumber,
        IPAddress
    }
    public NumericTypes NumericType;

    public ushort Port { get; private set; }

    private string m_PreviousValue;

    public string IPAddress
    {
        get
        {
            return m_InputField.text;
        }

    }

    private void Awake()
    {
        m_InputField = GetComponent<InputField>();
        m_PreviousValue = m_InputField.text;
    }

    public void EndEdit(string value)
    {
        switch (NumericType)
        {
            case NumericTypes.IPAddress:
                {
                    var ipAddress = new System.Net.IPAddress(0);

                    if (!value.Contains(".") || !System.Net.IPAddress.TryParse(value, out ipAddress))
                    {
                        m_InputField.text = m_PreviousValue;
                    }
                    else
                    {
                        m_PreviousValue = m_InputField.text;
                    }
                    break;
                }
        }
    }

    public void OnValueChanged(string value)
    {

    }

    private void OnGUI()
    {
        switch (NumericType)
        {
            case NumericTypes.PortNumber:
                {
                    var port = (ushort)0;
                    if (!ushort.TryParse(m_InputField.text, out port))
                    {
                        m_InputField.text = m_PreviousValue;
                    }
                    else
                    {
                        Port = port;
                        m_PreviousValue = m_InputField.text;
                    }

                    break;
                }
        }
    }
}
