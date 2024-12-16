using System.Globalization;
using System.Text;

public static class SearchVie
{
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Chuyển thành chữ thường để so sánh không phân biệt hoa thường
        text = text.ToLowerInvariant();

        // Chuyển đổi văn bản sang dạng chuẩn Unicode để tách dấu
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) // Không thêm các dấu phụ
            {
                stringBuilder.Append(c);
            }
        }

        // Trả về chuỗi không dấu và đã chuyển thành chữ thường
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
