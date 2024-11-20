namespace Aegis.Interfaces;

public interface IHardwareIdentifier
{
    string GetHardwareIdentifier();
    bool ValidateHardwareIdentifier(string hardwareIdentifier);
}