using System.Windows.Media.Imaging;

namespace DomainModels.ViewModel
{
    public enum SearchResultTypes
    {
        User = 1,
        Item = 2
    }

    public class SearchResult
    {
        //general
        public string FirstDisplayInfo { get; set; }
        public string SecondDisplayInfo { get; set; }
        public SearchResultTypes RecordType { get; set; }
        public byte[] Img { get; set; }
        //User part
        public int UserId { get; set; }
        public string FirstNameCh { get; set; }
        public string LastNameCh { get; set; }
        public string FirstNameEn { get; set; }
        public string LastNameEn { get; set; }
        //Item part
        public int ItemId { get; set; }
        public string Barcode { get; set; }
        public string Isbn { get; set; }
        public string Title { get; set; }
    }
}