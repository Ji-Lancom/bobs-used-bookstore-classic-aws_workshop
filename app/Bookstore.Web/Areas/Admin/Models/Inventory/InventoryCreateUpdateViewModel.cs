using Bookstore.Domain.ReferenceData;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Areas.Admin.Models.Inventory
{
    public class InventoryCreateUpdateViewModel
    {
        public InventoryCreateUpdateViewModel() { }

        public InventoryCreateUpdateViewModel(IEnumerable<ReferenceDataItem> referenceDataItems)
        {
            AddReferenceData(referenceDataItems);
        }

        public InventoryCreateUpdateViewModel(IEnumerable<ReferenceDataItem> referenceDataItems, Bookstore.Domain.Books.Book book) : this(referenceDataItems)
        {
            Author = book.Author;
            CoverImageUrl = book.CoverImageUrl;
            Id = book.Id;
            ISBN = book.ISBN;
            Name = book.Name;
            Price = book.Price;
            Quantity = book.Quantity;
            SelectedBookTypeId = book.BookTypeId;
            SelectedConditionId = book.ConditionId;
            SelectedGenreId = book.GenreId;
            SelectedPublisherId = book.PublisherId;
            Summary = book.Summary;
            Year = book.Year.GetValueOrDefault();
        }

        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string? Name { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string? Author { get; set; }

        public int Year { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string? ISBN { get; set; }

        public IEnumerable<SelectListItem> Publishers { get; set; } = new List<SelectListItem>();

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DisplayName("Publisher")]
        public int SelectedPublisherId { get; set; }

        public IEnumerable<SelectListItem> BookTypes { get; set; } = new List<SelectListItem>();

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DisplayName("Book Type")]
        public int SelectedBookTypeId { get; set; }

        public IEnumerable<SelectListItem> Genres { get; set; } = new List<SelectListItem>();

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DisplayName("Genre")]
        public int SelectedGenreId { get; set; }

        public IEnumerable<SelectListItem> BookConditions { get; set; } = new List<SelectListItem>();

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DisplayName("Condition")]
        public int SelectedConditionId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public decimal Price { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int Quantity { get; set; } = 1;

        [Bookstore.Web.Helpers.MaxFileSize(2 * 1024 * 1024)]
        [Bookstore.Web.Helpers.ImageTypes(new string[] { ".png", ".jpg", ".jpeg" })]
        [System.ComponentModel.DisplayName("Cover image")]
        public IFormFile? CoverImage { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? Summary { get; set; }

        public void AddReferenceData(IEnumerable<ReferenceDataItem> referenceDataItems)
        {
            BookConditions = referenceDataItems
                .Where(x => x.DataType == ReferenceDataType.Condition)
                .Select(x => new SelectListItem { Text = x.Text, Value = x.Id.ToString() });

            BookTypes = referenceDataItems
                .Where(x => x.DataType == ReferenceDataType.BookType)
                .Select(x => new SelectListItem { Text = x.Text, Value = x.Id.ToString() });

            Genres = referenceDataItems
                .Where(x => x.DataType == ReferenceDataType.Genre)
                .Select(x => new SelectListItem { Text = x.Text, Value = x.Id.ToString() });

            Publishers = referenceDataItems
                .Where(x => x.DataType == ReferenceDataType.Publisher)
                .Select(x => new SelectListItem { Text = x.Text, Value = x.Id.ToString() });
        }
    }
}
