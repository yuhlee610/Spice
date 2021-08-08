using Spice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public static class SD
    {
        public const string DefaultFoodImage = "default_food.png";
        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

        public const string ssCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";

        public const string StatusSubmitted = "Submitted";
        public const string StatusInProcess = "Being Prepared";
        public const string StatusReady = "Ready for Pickup";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusRejected = "Rejected";

        public const string OrderPlacedImage = "/images/OrderPlaced.png";
        public const string CompletedImage = "/images/completed.png";
        public const string InKitchenImage = "/images/InKitchen.png";
        public const string ReadyForPickupImage = "/images/ReadyForPickup.png";

        public static double Discounted(Coupon coupon, double OriginalOrderTotal)
        {
            if(coupon == null)
            {
                return OriginalOrderTotal;
            }
            else
            {
                if(coupon.MinimumAmount > OriginalOrderTotal)
                {
                    return OriginalOrderTotal;
                }
                else
                {
                    // valid
                    if(Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Dollar)
                    {
                        return Math.Round(OriginalOrderTotal - coupon.Discount, 2) > 0 ? Math.Round(OriginalOrderTotal - coupon.Discount, 2) : 0;
                    }
                    else
                    {
                        if (Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Percent)
                        {
                            return Math.Round(OriginalOrderTotal - OriginalOrderTotal * coupon.Discount / 100, 2);
                        }
                    }
                    return OriginalOrderTotal;
                }
            }
        }

        public static string ConvertToRawHtml(string source)
        {
            if(source == null)
            {
                return "Something here...";
            }
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }
}
