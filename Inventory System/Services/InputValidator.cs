namespace Inventory_System.Services;

public static class InputValidator
{
    public static string TrimSpaces(string value)
    {
        var text = value ?? string.Empty;
        var start = 0;
        var end = text.Length - 1;

        while (start <= end && (text[start] == ' ' || text[start] == '\t'))
        {
            start = start + 1;
        }

        while (end >= start && (text[end] == ' ' || text[end] == '\t'))
        {
            end = end - 1;
        }

        var built = string.Empty;
        for (var i = start; i <= end; i = i + 1)
        {
            built = built + text[i];
        }
        return built;
    }

    public static string RequiredError(string value, string fieldName)
    {
        var error = string.Empty;
        var text = value ?? string.Empty;
        var meaningful = 0;

        for (var i = 0; i < text.Length; i = i + 1)
        {
            var c = text[i];
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r') continue;
            meaningful = meaningful + 1;
        }

        if (meaningful == 0)
        {
            error = fieldName + " is required.";
        }
        return error;
    }

    public static string PasswordMatchError(string password, string confirm)
    {
        var error = string.Empty;

        if (password.Length != confirm.Length)
        {
            error = "Passwords do not match.";
        }
        else
        {
            for (var i = 0; i < password.Length; i = i + 1)
            {
                if (password[i] != confirm[i])
                {
                    error = "Passwords do not match.";
                }
            }
        }
        return error;
    }
}