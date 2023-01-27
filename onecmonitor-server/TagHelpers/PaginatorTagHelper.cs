using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Reflection.Emit;
using System.Text;

namespace OnecMonitor.Server.TagHelpers
{
    [HtmlTargetElement("nav", Attributes = "om-current-page,om-pages-count,om-filter")]
    public class PaginatorTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _generator;

        [HtmlAttributeName("om-current-page")]
        public int CurrentPage { get; set; }
        [HtmlAttributeName("om-pages-count")]
        public int PagesCount { get; set; }
        [HtmlAttributeName("om-filter")]
        public string Filter { get; set; } = string.Empty;
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext? ViewContext { get; set; }

        public PaginatorTagHelper(IHtmlGenerator generator)
        {
            _generator = generator;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "nav";
            output.TagMode = TagMode.StartTagAndEndTag;

            if (PagesCount <= 1)
            {
                output.Attributes.Add("hidden", "");
                return;
            }

            var qtyAfterBefore = 3;

            var startPage = CurrentPage - qtyAfterBefore;
            var finishPage = CurrentPage + qtyAfterBefore;

            if (startPage < 1)
            {
                finishPage += -(startPage - 1);
                startPage = 1;
            }

            if (finishPage > PagesCount)
            {
                startPage -= -(PagesCount - finishPage);
                finishPage = PagesCount;

                if (startPage < 1)
                    startPage = 1;
            }

            output.Content.AppendHtml("<ul class=\"pagination\">");

            // first page ref
            if (startPage > 1)
                AppendLi(1, "1", output);

            // fast stepping before
            if (startPage > 2)
                AppendLi(startPage - 1, "<<", output);

            // go to page buttons
            for (int i = startPage; i <= finishPage; i++)
                AppendLi(i, i.ToString(), output);

            // fast stepping after
            if (finishPage < PagesCount - 1)
                AppendLi(finishPage + 1, ">>", output);

            // last page ref
            if (finishPage < PagesCount)
                AppendLi(PagesCount, PagesCount.ToString(), output);

            output.Content.AppendHtml("</ul>");
        }

        private void AppendLi(int pageNumber, string pageName, TagHelperOutput output)
        {
            var active = pageNumber == CurrentPage ? " active" : "";

            output.Content.AppendHtml($"<li class=\"page-item{active}\">");

            var values = new Dictionary<string, string>
            {
                { "pageNumber", pageNumber.ToString() },
                { "filter", Filter }
            };
            var routeValues = new RouteValueDictionary(values!);

            var tagBuilder = _generator.GenerateRouteLink(
                ViewContext,
                linkText: pageName,
                routeName: string.Empty,
                hostName: string.Empty,
                protocol: string.Empty,
                fragment: string.Empty,
                routeValues: routeValues,
                htmlAttributes: null);

            tagBuilder.AddCssClass("page-link");

            output.Content.AppendHtml(tagBuilder.RenderStartTag());
            output.Content.AppendHtml(tagBuilder.RenderBody());
            output.Content.AppendHtml(tagBuilder.RenderEndTag());

            output.Content.AppendHtml("</li>");
        }
    }
}
