using System.Text;

namespace Aegis.Models;

public class Feature
{
    public FeatureValueType Type { get; init; }
    public byte[]? Data { get; init; }

    public static Feature FromBool(bool value) => new() { Type = FeatureValueType.Boolean, Data = BitConverter.GetBytes(value) };
    public static Feature FromInt(int value) => new() { Type = FeatureValueType.Integer, Data = BitConverter.GetBytes(value) };
    public static Feature FromFloat(float value) => new() { Type = FeatureValueType.Float, Data = BitConverter.GetBytes(value) };
    public static Feature FromString(string value) => new() { Type = FeatureValueType.String, Data = Encoding.UTF8.GetBytes(value) };
    public static Feature FromDateTime(DateTime value) => new() { Type = FeatureValueType.DateTime, Data = BitConverter.GetBytes(value.ToBinary()) };
    public static Feature FromByteArray(byte[] data) => new() { Type = FeatureValueType.ByteArray, Data = data };

    public bool AsBool() => Type == FeatureValueType.Boolean ? BitConverter.ToBoolean(Data!, 0) : throw new InvalidCastException("Feature is not a boolean.");
    public int AsInt() => Type == FeatureValueType.Integer ? BitConverter.ToInt32(Data!, 0) : throw new InvalidCastException("Feature is not an integer.");
    public float AsFloat() => Type == FeatureValueType.Float ? BitConverter.ToSingle(Data!, 0) : throw new InvalidCastException("Feature is not a float.");
    public string AsString() => Type == FeatureValueType.String ? Encoding.UTF8.GetString(Data!) : throw new InvalidCastException("Feature is not a string.");
    public DateTime AsDateTime() => Type == FeatureValueType.DateTime ? DateTime.FromBinary(BitConverter.ToInt64(Data!, 0)) : throw new InvalidCastException("Feature is not a DateTime.");
    public byte[] AsByteArray() => Type == FeatureValueType.ByteArray ? Data! : throw new InvalidCastException("Feature is not a byte array.");
}

public enum FeatureValueType
{
    Boolean,
    Integer,
    Float,
    String,
    DateTime,
    ByteArray
}