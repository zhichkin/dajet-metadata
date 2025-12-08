namespace DaJet.Data
{
    public static class DbUriHelper
    {
        public static bool UseExtensions(in Uri uri)
        {
            if (uri.Query is null)
            {
                return false;
            }

            string[] parameters = uri.Query.Split('?', '&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters is null || parameters.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                string[] parameter = parameters[i].Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parameter.Length == 1 && parameter[0] == "mdex")
                {
                    return true;
                }
            }

            return false;
        }
    }
}