using DoorgenCore.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Doorgen.Core.API.QwantApi;

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
        static Logger logger = LogManager.GetCurrentClassLogger();
        
        private wssEntities ctx;

        const int featuredThreshold = 5;
        private int featuredCount = 0;

        public wssEntities bxCtx { get { return ctx; } }
        public ImagesImportHelper()
        {
            ctx = new wssEntities();
        }

        // todo process images import into DB 
        // using wssModel
        Dictionary<int, string> parentCategories;
        Dictionary<int, string> childCategories;

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

        public void ReadCategories()
        {
            this.parentCategories = new Dictionary<int, string>();
            this.childCategories = new Dictionary<int, string>();

            var pCategories = ctx.avcms_wallpaper_categories.Where(c => c.parent == null).ToList();
            var cCategories = ctx.avcms_wallpaper_categories.Where(c => c.parent != null).ToList();

            foreach (avcms_wallpaper_categories category in pCategories)
                this.parentCategories.Add(Convert.ToInt32(category.id), category.name);

            foreach (avcms_wallpaper_categories category in cCategories)
                this.childCategories.Add(Convert.ToInt32(category.id), category.name);
        }

        public void InitDatabase()
        {
            //ALTER TABLE `wss`.`avcms_tag_taxonomy` 
            //ADD COLUMN `id` INT(5) NOT NULL AUTO_INCREMENT AFTER `content_type`,
            //ADD PRIMARY KEY(`id`);

            this.featuredCount = 0;

            ctx.avcms_wallpaper_categories.Clear();
            ctx.avcms_wallpapers.Clear();
            ctx.avcms_tag_taxonomy.Clear();
            ctx.avcms_tags.Clear();
            ctx.avcms_hits.Clear();
            bxCtx.SaveChanges();

            this.InitCategories();
        }

        public void InitCategories()
        {
            // clear Categories table and init it with default values
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

        private int DetectCategory(string[] keywords)
        {
            int categoryId = -1;

            // fill image category                        

            foreach (string keyword in keywords)
            {
                var category = this.childCategories.FirstOrDefault(kvp => kvp.Value.ToLower().Contains(keyword.ToLower()));

                if (this.childCategories.Contains(category))
                {
                    categoryId = category.Key;
                    break;
                }

                category = this.parentCategories.FirstOrDefault(kvp => kvp.Value.ToLower().Contains(keyword.ToLower()));
                if (this.parentCategories.Contains(category))
                {
                    categoryId = category.Key;
                    break;
                }
            }

            if (categoryId == -1)
            {
                // no categories match, get the random category
                Random rand = new Random();
                categoryId = parentCategories.Skip(rand.Next(parentCategories.Count)).First().Key;
            }

            return categoryId;
        }

        private void processTags(long imageId, string[] keywords)
        {
            foreach (string tag in keywords)
            {
                // skip garbage 
                if (tag.Length < 3)
                    continue;

                // skip 'wallpaper' keyword
                if (tag.ToLower().Contains("wallpaper"))
                    continue;

                // skip WxH type keyword
                string[] wh = tag.ToLower().Split('x');
                if (wh.Length == 2)
                    continue;

                if (tag.All(char.IsDigit))
                    continue;

                long tagId = -1;

                var dbTag = bxCtx.avcms_tags.Where(t => t.name.Contains(tag)).FirstOrDefault();
                if (dbTag != null)
                    tagId = dbTag.id;
                else
                {
                    avcms_tags newDbTag = new avcms_tags()
                    {
                        name = tag
                    };

                    bxCtx.avcms_tags.Add(newDbTag);
                    bxCtx.SaveChanges();
                    tagId = newDbTag.id;
                }

                avcms_tag_taxonomy tagTaxonomy = new avcms_tag_taxonomy()
                {
                    content_id = Convert.ToInt32(imageId),
                    taxonomy_id = Convert.ToInt32(tagId),
                    content_type = "wallpaper"
                };

                bxCtx.avcms_tag_taxonomy.Add(tagTaxonomy);
                bxCtx.SaveChanges();
            }
        }

        public void ImportImage(QwantImage image)
        {
            // import image into avcms_wallpapers table
            logger.Info("- импорт картинки в CMS");

            int isFeatured = this.featuredCount > featuredThreshold ? 1 : 0;

            string[] keywords = image.keywords.Split(' ');
            int categoryId = this.DetectCategory(keywords);                        

            Random rand1 = new Random();
            int total_downloads = rand1.Next(100);
            int likes = rand1.Next(100);
            int dislikes = rand1.Next(10);
            int hits = rand1.Next(1000);

            int daysAdded = rand1.Next(90);
            DateTime foo = DateTime.UtcNow.AddDays(0 - daysAdded);
            long unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds();

            // import image into db
            avcms_wallpapers avcmsWallpaper = new avcms_wallpapers()
            {
                creator_id = 1,
                name = image.keywords,
                description = image.title,
                file = image.file,
                category_id = categoryId,
                date_added = unixTime,
                published = true,
                publish_date = unixTime,
                slug = image.keywords.Replace(" ","-"),
                resize_type = "crop",
                crop_position = "center",
                original_width = image.width,
                original_height = image.height,
                featured = isFeatured,
                total_downloads = total_downloads,
                likes = likes,
                dislikes = dislikes,
                hits = hits                
            };

            bxCtx.avcms_wallpapers.Add(avcmsWallpaper);
            bxCtx.SaveChanges();

            // fill image tags            
            this.processTags(avcmsWallpaper.id, keywords);

            this.featuredCount = this.featuredCount <= featuredThreshold ? this.featuredCount + 1 : 0;            
        }
    }
}
