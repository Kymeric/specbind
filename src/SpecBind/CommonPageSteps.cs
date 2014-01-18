﻿// <copyright file="CommonPageSteps.cs">
//    Copyright © 2013 Dan Piessens  All rights reserved.
// </copyright>

namespace SpecBind
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using SpecBind.ActionPipeline;
	using SpecBind.Actions;
	using SpecBind.BrowserSupport;
	using SpecBind.Helpers;
	using SpecBind.Pages;
	
	using TechTalk.SpecFlow;

	/// <summary>
	/// A set of common step bindings that drive the underlying fixtures.
	/// </summary>
	[Binding]
	public class CommonPageSteps : PageStepBase
	{
		// Step regex values - in constants because they are shared.
		private const string EnsureOnPageStepRegex = @"I am on the (.+) page";
		private const string EnsureOnDialogStepRegex = @"I am on the (.+) dialog";
		private const string EnterDataInFieldsStepRegex = @"I enter data";
        private const string NavigateToPageStepRegex = @"I navigate to the (.+) page";
        private const string NavigateToPageWithParamsStepRegex = @"I navigate to the (.+) page with parameters";
		private const string ObserveDataStepRegex = @"I see";
		private const string ObserveListDataStepRegex = @"I see (.+) list ([A-Za-z ]+)";
		private const string ChooseALinkStepRegex = @"I choose (.+)";
		private const string SetTokenFromFieldRegex = @"I set token (.+) with the value of (.+)";

		// The following Regex items are for the given "past tense" form
		private const string GivenEnsureOnPageStepRegex = @"I was on the (.+) page";
		private const string GivenEnsureOnDialogStepRegex = @"I was on the (.+) dialog";
		private const string GivenEnterDataInFieldsStepRegex = @"I entered data";
		private const string GivenObserveDataStepRegex = @"I saw";
		private const string GivenObserveListDataStepRegex = @"I saw (.+) list ([A-Za-z ]+)";
		private const string GivenChooseALinkStepRegex = @"I chose (.+)";
		private const string GivenNavigateToPageStepRegex = @"I navigated to the (.+) page";
		private const string GivenNavigateToPageWithParamsStepRegex = @"I navigated to the (.+) page with parameters";
		
		private readonly IBrowser browser;
		private readonly IPageMapper pageMapper;

	    private readonly IActionPipelineService actionPipelineService;

	    /// <summary>
	    /// Initializes a new instance of the <see cref="CommonPageSteps" /> class.
	    /// </summary>
	    /// <param name="browser">The browser.</param>
	    /// <param name="pageMapper">The page mapper.</param>
	    /// <param name="scenarioContext">The scenario context.</param>
	    /// <param name="actionPipelineService">The action pipeline service.</param>
	    public CommonPageSteps(IBrowser browser, IPageMapper pageMapper, IScenarioContextHelper scenarioContext, IActionPipelineService actionPipelineService)
            : base(scenarioContext)
		{
			this.browser = browser;
			this.pageMapper = pageMapper;
			this.actionPipelineService = actionPipelineService;
		}

		/// <summary>
		/// A Given step for ensuring the browser is on the page with the specified name.
		/// </summary>
		/// <param name="pageName">The page name.</param>
		[Given(GivenEnsureOnPageStepRegex)]
        [When(EnsureOnPageStepRegex)]
		[Then(EnsureOnPageStepRegex)]
		public void GivenEnsureOnPageStep(string pageName)
		{
			var type = this.GetPageType(pageName);

			IPage page;
			this.browser.EnsureOnPage(type, out page);

            this.UpdatePageContext(page);
		}

		/// <summary>
		/// A Given step for ensuring the browser is on the dialog which is a sub-element of the page.
		/// </summary>
		/// <param name="propertyName">Name of the property that represents the dialog.</param>
		[Given(GivenEnsureOnDialogStepRegex)]
		[When(EnsureOnDialogStepRegex)]
		[Then(EnsureOnDialogStepRegex)]
		public void GivenEnsureOnDialogStep(string propertyName)
		{
			var page = this.GetPageFromContext();

            var context = new ActionContext(propertyName.ToLookupKey());
            var item = this.actionPipelineService.PerformAction<GetElementAsPageAction>(page, context)
                                                 .CheckResult<IPage>();

            this.UpdatePageContext(item);
		}

		/// <summary>
		/// A Given step for navigating to a page with the specified name.
		/// </summary>
		/// <param name="pageName">The page name.</param>
		[Given(GivenNavigateToPageStepRegex)]
        [When(NavigateToPageStepRegex)]
        [Then(NavigateToPageStepRegex)]
		public void GivenNavigateToPageStep(string pageName)
		{
			this.GivenNavigateToPageWithArgumentsStep(pageName, null);
		}

		/// <summary>
		/// A Given step for navigating to a page with the specified name and url parameters.
		/// </summary>
		/// <param name="pageName">The page name.</param>
		/// <param name="pageArguments">The page arguments.</param>
		[Given(GivenNavigateToPageWithParamsStepRegex)]
        [When(NavigateToPageWithParamsStepRegex)]
        [Then(NavigateToPageWithParamsStepRegex)]
		public void GivenNavigateToPageWithArgumentsStep(string pageName, Table pageArguments)
		{
			var type = this.GetPageType(pageName);

			Dictionary<string, string> args = null;
			if (pageArguments != null && pageArguments.RowCount > 0)
			{
				args = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				var row = pageArguments.Rows[0];
				foreach (var header in pageArguments.Header.Where(h => !args.ContainsKey(h)))
				{
					args.Add(header, row[header]);
				}
			}

			var page = this.browser.GoToPage(type, args);
            this.UpdatePageContext(page);
		}

		/// <summary>
		/// A When step indicating a link click should occur.
		/// </summary>
		/// <param name="linkName">Name of the link.</param>
		[Given(GivenChooseALinkStepRegex)]
		[When(ChooseALinkStepRegex)]
		public void WhenIChooseALinkStep(string linkName)
		{
			var page = this.GetPageFromContext();

            var context = new ActionContext(linkName.ToLookupKey());
			
            this.actionPipelineService
                    .PerformAction<ButtonClickAction>(page, context)
                    .CheckResult();
		}

		/// <summary>
		/// A When step for entering data into fields.
		/// </summary>
		/// <param name="data">The field data.</param>
		[Given(GivenEnterDataInFieldsStepRegex)]
		[When(EnterDataInFieldsStepRegex)]
		[Then(EnterDataInFieldsStepRegex)]
		public void WhenIEnterDataInFieldsStep(Table data)
		{
			string fieldHeader = null;
			string valueHeader = null;

			if (data != null)
			{
				fieldHeader = data.Header.FirstOrDefault(h => h.NormalizedEquals("Field"));
				valueHeader = data.Header.FirstOrDefault(h => h.NormalizedEquals("Value"));
			}

			if (fieldHeader == null || valueHeader == null)
			{
				throw new ElementExecuteException("A table must be specified for this step with the columns 'Field' and 'Value'");
			}

			var page = this.GetPageFromContext();

		    var results = new List<ActionResult>(data.RowCount);
		    results.AddRange(from tableRow in data.Rows 
                                           let fieldName = tableRow[fieldHeader]
                                           let fieldValue = tableRow[valueHeader] 
                                           select new EnterDataAction.EnterDataContext(fieldName.ToLookupKey(), fieldValue) into context 
                                           select this.actionPipelineService.PerformAction<EnterDataAction>(page, context));

		    if (results.Any(r => !r.Success))
		    {
		        var errors = string.Join("; ", results.Where(r => r.Exception != null).Select(r => r.Exception.Message));
		        throw new ElementExecuteException("Errors occurred while entering data. Details: {0}", errors);   
		    }
		}

		/// <summary>
		/// A Then step
		/// </summary>
		/// <param name="data">The field data.</param>
		[Given(GivenObserveDataStepRegex)]
		[Then(ObserveDataStepRegex)]
		public void ThenISeeStep(Table data)
		{
		    var validations = data.ToValidationTable();
			var page = this.GetPageFromContext();

            var context = new ValidateItemAction.ValidateItemContext(validations);
            this.actionPipelineService.PerformAction<ValidateItemAction>(page, context).CheckResult();
		}

		/// <summary>
		/// A Then step for validating items in a list.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="rule">The rule.</param>
		/// <param name="data">The field data.</param>
		/// <exception cref="ElementExecuteException">A table must be specified for this step with the columns 'Field', 'Rule' and 'Value'</exception>
		[Given(GivenObserveListDataStepRegex)]
		[Then(ObserveListDataStepRegex)]
		public void ThenISeeListStep(string fieldName, string rule, Table data)
		{
			if (data == null || data.RowCount == 0)
			{
				return;
			}

			ComparisonType comparisonType;
			switch (rule.ToLookupKey())	
			{
				case "exists":
				case "contains":
					comparisonType = ComparisonType.Contains;
					break;

				case "doesnotexist":
				case "doesnotcontain":
					comparisonType = ComparisonType.DoesNotContain;
					break;

				case "startswith":
					comparisonType = ComparisonType.StartsWith;
					break;

				case "endswith":
					comparisonType = ComparisonType.EndsWith;
					break;
                case "equals":
                    comparisonType = ComparisonType.Equals;
			        break;
				default:
					throw new InvalidOperationException(string.Format("Rule type '{0}' is not supported.", rule));
			}

			var page = this.GetPageFromContext();
		    var validations = data.ToValidationTable();

		    var context = new ValidateListAction.ValidateListContext(fieldName.ToLookupKey(), comparisonType, validations);
            this.actionPipelineService.PerformAction<ValidateListAction>(page, context).CheckResult();
		}

		/// <summary>
		/// Sets the token specified from the given property value.
		/// </summary>
		/// <param name="tokenName">Name of the token.</param>
		/// <param name="propertyName">Name of the property.</param>
		[Given(SetTokenFromFieldRegex)]
		[When(SetTokenFromFieldRegex)]
		[Then(SetTokenFromFieldRegex)]
		public void SetTokenFromFieldStep(string tokenName, string propertyName)
		{
			var page = this.GetPageFromContext();

            var context = new SetTokenFromValueAction.TokenFieldContext(propertyName.ToLookupKey(), tokenName);

            this.actionPipelineService
                .PerformAction<SetTokenFromValueAction>(page, context)
                .CheckResult();
		}

		/// <summary>
		/// Gets the type of the page.
		/// </summary>
		/// <param name="pageName">Name of the page.</param>
		/// <returns>The page type.</returns>
		/// <exception cref="PageNavigationException">Thrown if the page type cannot be found.</exception>
		private Type GetPageType(string pageName)
		{
			var type = this.pageMapper.GetTypeFromName(pageName);

			if (type == null)
			{
				throw new PageNavigationException(
					"Cannot locate a page for name: {0}. Check page aliases in the test assembly.", pageName);
			}

			return type;
		}
	}
}