namespace vsl
{
    public static class StringExtensions
    {
        public static int Position(this string text)
        {
            return text.Split('\n').Length - 1;
        }
    }
}