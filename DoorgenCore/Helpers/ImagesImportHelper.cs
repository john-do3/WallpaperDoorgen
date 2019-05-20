using DoorgenCore.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorgenCore.Helpers
{
    public static class EntityExtensions
    {
        public static void Clear<T>(this DbSet<T> dbSet) where T : class
        {
            dbSet.RemoveRange(dbSet);
        }
    }

    class ImagesImportHelper
    {
        private wssEntities ctx;

        public wssEntities bxCtx { get { return ctx; } }
        public ImagesImportHelper()
        {
            ctx = new wssEntities();
        }

        // todo process images import into DB 
        // using wssModel

        Dictionary<string, List<string>> categories = new Dictionary<string, List<string>>() {
            {"Abstract", new List<string>{"3D", "Dark", "Fantasy", "Fractal", "Humor", "Sci-Fi", "Texts", "Texture", "Vector" } },
            {"Animals", new List<string> { "Baby Animals", "Birds", "Cats", "Dogs", "Fishes", "Insects", "Reptiles" } },
            {"Entertainment", new List<string> { "Anime", "Cartoons", "Classic Games", "Comics", "Movies", "Music", "TV Series", "Videogames" } },
            {"Holidays", new List<string> { "Birthday", "Christmas", "Easter", "Halloween", "New Year", "Valentine's Day" } },
            {"Nature", new List<string> { "Beaches", "Deserts", "Drops", "Flowers", "Fruits", "Lakes","Landscapes", "Leaves", "Mountains", "Plants", "Rivers", "Sea", "Ocean", "Sky", "Space", "Sunrise", "Sunset", "Trees", "Waterfalls" } },
            {"People", new List<string> { "Children", "Males", "Females" } },
            {"Vehicles", new List<string> { "Aircrafts", "Cars", "Concepts", "Motorcycles", "Trains", "Trucks", "Watercrafts" } },
            {"World", new List<string> { "Architecture", "Cities", "Flags", "Roads" } }
        };

        public void InitCategories()
        {
            // clear Categories table and init it with default values

            ctx.avcms_wallpaper_categories.Clear();

            foreach (KeyValuePair<string, List<string>> kvp in this.categories)
            {
                string mainCategory = kvp.Key;

                avcms_wallpaper_categories avcmsMainCategory = new avcms_wallpaper_categories()
                {
                    name = mainCategory,
                    slug = mainCategory.ToLower()
                };

                ctx.avcms_wallpaper_categories.Add(avcmsMainCategory);
                ctx.SaveChanges();
                long parentId = avcmsMainCategory.id;

                string children = "";
                
                foreach (string subCategory in kvp.Value)
                {
                    string category = $"{mainCategory} | {subCategory}";
                    avcms_wallpaper_categories avcmsSubCategory = new avcms_wallpaper_categories()
                    {                        
                        name = subCategory,
                        slug = $"{mainCategory.ToLower()}-{subCategory.ToLower()}",
                        parent = Convert.ToInt32(parentId),
                        parents = parentId.ToString()
                    };

                    ctx.avcms_wallpaper_categories.Add(avcmsSubCategory);
                    ctx.SaveChanges();

                    children += $"{avcmsSubCategory.id.ToString()},";
                }

                avcmsMainCategory.children = children;
                ctx.SaveChanges();
            }
        }
    }
}
