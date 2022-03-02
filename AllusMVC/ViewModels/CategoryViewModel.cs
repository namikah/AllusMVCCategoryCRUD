using AllusMVC.Models;
using System.Collections.Generic;

namespace AllusMVC.ViewModels
{
    public class CategoryViewModel
    {
        public Category SelectedCategory { get; set; }

        public List<Category> Categories { get; set; }
    }
}
