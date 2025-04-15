namespace TruckManagement.Helpers;

public static class ContractAccessCodeGenerator
{
    public static string Generate()
    {
        var random = new Random();
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var chars = new char[6];

        for (int i = 0; i < 3; i++)
            chars[i] = letters[random.Next(letters.Length)];

        for (int i = 3; i < 6; i++)
            chars[i] = (char)('0' + random.Next(10));

        return new string(chars);
    }
}