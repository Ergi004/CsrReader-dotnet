namespace CsvReader.Models.CsvFile
{ 
   public class CsvProcessingResult
    {
        public string FileName { get; set; } = string.Empty;
        public int RowsProcessed { get; set; }
        public List<string> Descriptions { get; set; } = new();
        public string UpdatedCsvContent { get; set; } = string.Empty;
        public string SavedFilePath { get; set; } = string.Empty;
    }


}
