using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Soundboard.Avalonia.UI.Views
{
    public partial class CategoryBarView : UserControl
    {

        public ObservableCollection<DesignCategoryModel> Categories { get; set; } = new([
            new DesignCategoryModel
            {
                Name = "All",
                BackgroundBrush = new SolidColorBrush(0xFF818CF8),
                IsAll = true,
                IsEnabled = true,
                SoundCount = 45
            },
            new DesignCategoryModel
            {
                Name = "Basses",
                BackgroundBrush = new SolidColorBrush(0xFFFBBF24),
                IsAll = false,
                IsEnabled = true,
                SoundCount = 7
            },
            new DesignCategoryModel
            {
                Name = "Cymbals",
                BackgroundBrush = new SolidColorBrush(0xFF34D399),
                IsAll = false,
                IsEnabled = true,
                SoundCount = 8

            },
            new DesignCategoryModel
            {
                Name = "Drum Loops",
                BackgroundBrush = new SolidColorBrush(0xFF38BDF8),
                IsAll = false,
                IsEnabled = true,
                SoundCount = 13
            },
            new DesignCategoryModel
            {
                Name = "Kicks",
                BackgroundBrush = new SolidColorBrush(0xFFF472B6),
                IsAll = false,
                IsEnabled = true,
                SoundCount = 4
            },
            new DesignCategoryModel
            {
                Name = "Perks",
                BackgroundBrush = new SolidColorBrush(0xFFFB923C),
                IsAll = false,
                IsEnabled = false,
                SoundCount = 3
            },
            new DesignCategoryModel
            {
                Name = "Snares",
                BackgroundBrush = new SolidColorBrush(0xFF94A3B8),
                IsAll = false,
                IsEnabled = false,
                SoundCount = 6
            }
        ]);

        public CategoryBarView()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: DesignCategoryModel category })
                category.IsEnabled = !category.IsEnabled;
        }
    }
}