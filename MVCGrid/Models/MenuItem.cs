namespace MVCGrid.Models
{
    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsNew { get; set; }
        public List<MenuItem>? SubItems { get; set; }

        public static MenuItem Create(string title, string url, string icon, bool isNew = false)
        {
            return new MenuItem
            {
                Title = title,
                Url = url,
                Icon = icon,
                IsNew = isNew
            };
        }

        public static MenuItem CreateWithSubmenu(string title, string icon, params MenuItem[] subItems)
        {
            return new MenuItem
            {
                Title = title,
                Url = "#",
                Icon = icon,
                SubItems = subItems.ToList()
            };
        }
    }
}
