using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PKHeX.Core;

namespace PKHeX.Drawing.Misc;

/// <summary>
/// Provides utilities for decoding QR codes using an external web API.
/// </summary>
public static class QRDecode
{
    // QR Utility
    private const string DecodeAPI = "http://api.qrserver.com/v1/read-qr-code/?fileurl=";

    /// <summary>
    /// Retrieves and decodes QR code data from the specified image URL.
    /// </summary>
    /// <param name="address">The URL of the image containing the QR code.</param>
    /// <param name="result">The decoded byte array result, if successful.</param>
    /// <returns>A <see cref="QRDecodeResult"/> indicating the outcome of the operation.</returns>
    public static QRDecodeResult GetQRData(string address, out byte[] result)
    {
        result = [];
        // Fetch data from QR code...

        if (!address.StartsWith("http"))
            return QRDecodeResult.BadPath;

        string url = DecodeAPI + WebUtility.UrlEncode(address);
        string data;
        try
        {
            var str = NetUtil.GetStringFromURL(new Uri(url));
            if (str is null)
                return QRDecodeResult.BadConnection;

            data = str;
            if (data.Contains("could not find"))
                return QRDecodeResult.BadImage;

            if (data.Contains("filetype not supported"))
                return QRDecodeResult.BadType;
        }
        catch { return QRDecodeResult.BadConnection; }

        // Quickly convert the json response to a data string
        try
        {
            result = DecodeQRJson(data);
            return QRDecodeResult.Success;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return QRDecodeResult.BadConversion;
        }
    }

    /// <summary>
    /// Decodes the JSON response from the QR code API into a byte array.
    /// </summary>
    /// <param name="data">The JSON response string from the API.</param>
    /// <returns>The decoded byte array from the QR code.</returns>
    /// <exception cref="FormatException">Thrown if the JSON format is invalid or unexpected.</exception>
    private static byte[] DecodeQRJson(string data)
    {
        const string cap = "\",\"error\":null}]}]";
        const string intro = "[{\"type\":\"qrcode\",\"symbol\":[{\"seq\":0,\"data\":\"";
        const string qrcode = "nQR-Code:";
        if (!data.StartsWith(intro))
            throw new FormatException();

        string pkstr = data[intro.Length..];

        // Remove multiple QR codes in same image
        var qr = pkstr.IndexOf(qrcode, StringComparison.Ordinal);
        if (qr != -1)
            pkstr = pkstr[..qr];

        // Trim outro
        var outroIndex = pkstr.IndexOf(cap, StringComparison.Ordinal);
        if (outroIndex == -1)
            throw new FormatException();

        pkstr = pkstr[..outroIndex];

        if (!pkstr.StartsWith("http") && !pkstr.StartsWith("null")) // G7
        {
            string fstr = Regex.Unescape(pkstr);
            byte[] raw = Encoding.Unicode.GetBytes(fstr);

            // Remove 00 interstitials and retrieve from offset 0x30, take PK7 Stored Size (always)
            byte[] result = new byte[0xE8];
            for (int i = 0; i < result.Length; i++)
                result[i] = raw[(i + 0x30) * 2];
            return result;
        }
        // All except G7
        pkstr = pkstr[(pkstr.IndexOf('#') + 1)..]; // Trim URL
        pkstr = pkstr.Replace("\\", string.Empty); // Rectify response

        return Convert.FromBase64String(pkstr);
    }

    /// <summary>
    /// Converts a <see cref="QRDecodeResult"/> to a user-friendly message string.
    /// </summary>
    /// <param name="result">The QR decode result.</param>
    /// <returns>A message string describing the result, or an empty string if successful.</returns>
    public static string ConvertMsg(this QRDecodeResult result) => result switch
    {
        QRDecodeResult.Success => string.Empty,
        QRDecodeResult.BadPath => MessageStrings.MsgQRUrlFailPath,
        QRDecodeResult.BadImage => MessageStrings.MsgQRUrlFailImage,
        QRDecodeResult.BadType => MessageStrings.MsgQRUrlFailType,
        QRDecodeResult.BadConnection => MessageStrings.MsgQRUrlFailConnection,
        QRDecodeResult.BadConversion => MessageStrings.MsgQRUrlFailConvert,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };
}
