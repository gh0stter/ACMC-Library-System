using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMC_Library_System.DbModels;

namespace ACMC_Library_System.Supports
{
    internal static class Cache
    {
        public static List<item_category> ItemCategories { get; set; }
        public static List<item_class> ItemClasses { get; set; }
        public static List<item_status> ItemStatuses { get; set; }
        public static List<patron> Members { get; set; }
        public static List<item> Items { get; set; }
        public static List<action_history> ActionHistories { get; set; }
        public static List<item> ItemsShouldReturn { get; set; }

        private static Task<T> AsyncWrap<T>(T expression)
        {
            return Task.Run(() => expression);
        }

        /// <summary>
        /// Refresh all cache data from database
        /// </summary>
        /// <returns></returns>
        public static async Task RefreshAllCache()
        {
            await Task.Run(async () =>
            {
                using (var context = new LibraryDb())
                {
                    ItemCategories = await AsyncWrap(context.item_category.ToList());
                    ItemClasses = await AsyncWrap(context.item_class.ToList());
                    ItemStatuses = await AsyncWrap(context.item_status.ToList());
                    Members = await AsyncWrap(context.patron.ToList());
                    Items = await AsyncWrap(context.item.ToList());
                    ActionHistories = await AsyncWrap(context.action_history.OrderByDescending(i => i.id).Take(20).ToList());
                    Parallel.ForEach(ActionHistories,
                                     action =>
                                     {
                                         action.MemberName = Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                                         action.ItemName = Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                                         action.ActionType = ((action_type.ActionTypeEnum)action.action_type).ToString();
                                     });
                    ItemsShouldReturn = Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).ToList();
                    Parallel.ForEach(ItemsShouldReturn,
                                     item =>
                                     {
                                         item.Borrower = Members.FirstOrDefault(i => i.id == item.patronid);
                                     });
                }
            });
        }

        /// <summary>
        /// Refresh Main (member/item/history/itemsShouldReturn) cache from database
        /// </summary>
        /// <returns></returns>
        public static async Task RefreshMainCache()
        {
            await Task.Run(async () =>
            {
                using (var context = new LibraryDb())
                {
                    Members = await AsyncWrap(context.patron.ToList());
                    Items = await AsyncWrap(context.item.ToList());
                    ActionHistories = await AsyncWrap(context.action_history.OrderByDescending(i => i.id).Take(20).ToList());
                    Parallel.ForEach(ActionHistories,
                                     action =>
                                     {
                                         action.MemberName = Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                                         action.ItemName = Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                                         action.ActionType = ((action_type.ActionTypeEnum)action.action_type).ToString();
                                     });
                    ItemsShouldReturn = await AsyncWrap(Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).ToList());
                    Parallel.ForEach(ItemsShouldReturn,
                                     item =>
                                     {
                                         item.Borrower = Members.FirstOrDefault(i => i.id == item.patronid);
                                     });
                }
            });
        }
    }
}
