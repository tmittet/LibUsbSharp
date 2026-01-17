namespace UsbDotNet.Transfer;

public enum ControlRequestRecipient : byte
{
    /// <summary>
    /// The request affects the whole device.
    /// </summary>
    Device = 0,

    /// <summary>
    /// The request targets a specific interface.
    /// </summary>
    Interface = 1,

    /// <summary>
    /// The request targets a specific endpoint.
    /// </summary>
    Endpoint = 2,

    /// <summary>
    /// The request targets "other" elements defined by a class spec (not an interface or endpoint).
    /// </summary>
    Other = 3,
}
