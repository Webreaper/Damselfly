using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// View Wrapper for a folder being displayed in the list-view
    /// item. We create/load this once and it provides bindable
    /// propeties that will simplify the view code. 
    /// </summary>
	public class FolderListItem
	{
		public Folder Folder { get; set; }
        public string DisplayName { get; set; }
		public int ImageCount { get; set; }
		public DateTime MaxImageDate { get; set; }

        public override string ToString()
        {
            return $"{Folder.Path} [{ImageCount} images, most recent date {MaxImageDate}]";
        }
    }
}
