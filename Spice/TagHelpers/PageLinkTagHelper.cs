using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Spice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.TagHelpers
{
    [HtmlTargetElement("div", Attributes = "page-model")]
    public class PageLinkTagHelper : TagHelper
    {
        private IUrlHelperFactory urlHelperFactory;

        public PageLinkTagHelper(IUrlHelperFactory helperFactory)
        {
            urlHelperFactory = helperFactory;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public PagingInfo PageModel { get; set; }
        public string PageAction { get; set; }
        public bool PageClassesEnabled { get; set; }
        public string PageClass { get; set; }
        public string PageClassNormal { get; set; }
        public string PageClassSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            TagBuilder result = new TagBuilder("div");

            // Create Previous button
            if(PageModel.CurrentPage > 1)
            {
                TagBuilder tag = new TagBuilder("a");
                string url = PageModel.urlParam.Replace(":", (PageModel.CurrentPage - 1).ToString());
                tag.Attributes["href"] = url;
                tag.AddCssClass(PageClass);
                tag.AddCssClass(PageClassNormal);
                tag.InnerHtml.AppendHtml("Previous");
                result.InnerHtml.AppendHtml(tag);
            }

            // Create pagination link : page 1, page 2, ...
            for (int i = 1; i <= PageModel.TotalPage; i++)
            {
                TagBuilder tag = new TagBuilder("a");
                string url = PageModel.urlParam.Replace(":", i.ToString());
                tag.Attributes["href"] = url;
                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);
                    tag.AddCssClass(i == PageModel.CurrentPage ? PageClassSelected : PageClassNormal);
                }
                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            // Create Next button
            if (PageModel.CurrentPage > 0 && PageModel.CurrentPage < PageModel.TotalPage)
            {
                TagBuilder tag = new TagBuilder("a");
                string url = PageModel.urlParam.Replace(":", (PageModel.CurrentPage + 1).ToString());
                tag.Attributes["href"] = url;
                tag.AddCssClass(PageClass);
                tag.AddCssClass(PageClassNormal);
                tag.InnerHtml.AppendHtml("Next");
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }

    }
}
