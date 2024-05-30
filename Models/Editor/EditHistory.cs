using System.Data;

namespace RHToolkit.Models.Editor
{
    public class EditHistory
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DataRow? AffectedRow { get; set; }
        public EditAction Action { get; set; }
        public List<EditHistory>? GroupedEdits { get; set; }
    }

    public enum EditAction
    {
        CellEdit,
        RowInsert,
        RowDelete
    }

}
