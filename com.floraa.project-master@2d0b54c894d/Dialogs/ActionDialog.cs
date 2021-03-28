// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{


    public class ActionDialog : CancelAndHelpDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public ActionDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(ActionDialog))
        {
            Configuration = configuration;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                PortfolioStepAsync,
                EnvironmentStepAsync,
                TagStepAsync,
                DbInstanceStepAsync,
                ConfirmStepAsync,
                CaptureEmailStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Intent == "Acronym")
            {
                if (entitiDetails.Acronym == null && entitiDetails.Score > 0.3)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Which acronym details would you like to have?") }, cancellationToken);
                }
                if (entitiDetails.Score < 0.3 && entitiDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word") }, cancellationToken);
                }
                else
                {
                    return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
                }
            }
            else if (entitiDetails.Intent == "Trigger_Service" || entitiDetails.Intent == "Build_Deployment")
            {
                if ((entitiDetails.Project == null || entitiDetails.Project == "ProductionHealthCheck") && entitiDetails.Score > 0.3 && entitiDetails.Portfolio == null)
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text("Please enter the portfolio"),
                         Choices = ChoiceFactory.ToChoices(new List<string> { "PCA", "CCV", "Rapid" }),
                         RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                     }, cancellationToken);

                }
                else if (entitiDetails.Score < 0.3 && entitiDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other Project") }, cancellationToken);
                }
                //else if (entitiDetails.Tag == "AutoDerivation")
                //{
                //    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                //     new PromptOptions
                //     {
                //         Prompt = MessageFactory.Text("Please select the environment"),
                //         Choices = ChoiceFactory.ToChoices(new List<string> { "QA", "UAT", "PROD" }),
                //         RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                //     }, cancellationToken);
                //}
                else
                {
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
            }

        }
        private async Task<DialogTurnResult> PortfolioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Trigger_Service":
                    if (!string.IsNullOrEmpty(entitiDetails.Project))
                    {
                        if (entitiDetails.Tag == "AutoDerivation" && string.IsNullOrEmpty(entitiDetails.Environment))
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the environment"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> {"DEV", "QA", "UAT", "PROD" }),
                                 Style=ListStyle.Auto,
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        if (entitiDetails.Project.ToUpper() == "PRODUCTIONHEALTHCHECK" && string.IsNullOrEmpty(entitiDetails.Portfolio))
                            entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                    entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                    //if (entitiDetails.Project == "ProductionHealthCheck")
                    //    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    if (entitiDetails.Portfolio.ToUpper() == "PCA")
                    {

                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Please select the project"),
                   Choices = GetProjectChoices(entitiDetails.Portfolio),
                   Style = ListStyle.List,
                   RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
               }, cancellationToken);
                    }
                    else if (entitiDetails.Portfolio.ToUpper() == "CCV")
                    {

                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Please select the project"),
                   Choices = GetProjectChoices(entitiDetails.Portfolio),
                   Style = ListStyle.Auto,
                   RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
               }, cancellationToken);
                    }
                    else if (entitiDetails.Tag.Contains("AutoDerivation"))
                    {

                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Still we are working on to integrate the Rapid projects with floraa. Please try for PCA projects"), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                case "Build_Deployment":
                    if (!string.IsNullOrEmpty(entitiDetails.Project))
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    //entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
              new PromptOptions
              {
                  Prompt = MessageFactory.Text("Please select the project"),
                  Choices = GetProjectChoices(entitiDetails.Intent),
                  Style = ListStyle.Auto,
                  RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
              }, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> EnvironmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Trigger_Service":
                    if (entitiDetails.Tag == "AutoDerivation" && string.IsNullOrEmpty(entitiDetails.Environment))
                        entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (string.IsNullOrEmpty(entitiDetails.Project))
                        entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                case "Build_Deployment":
                    if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Environment))
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    else if (string.IsNullOrEmpty(entitiDetails.Project))
                        entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                            new PromptOptions
                            {
                                Prompt = MessageFactory.Text("Please select the environment"),
                                Choices = ChoiceFactory.ToChoices(new List<string> { "QA", "UAT", "PROD" }),
                                RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                            }, cancellationToken);
                //entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();

                default:
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> TagStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Tag) && entitiDetails.Intent == "Trigger_Service")
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Buildwar) && entitiDetails.Intent == "Build_Deployment")
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            else if (!string.IsNullOrEmpty(entitiDetails.Project) && string.IsNullOrEmpty(entitiDetails.Tag))
                if (entitiDetails.Project.ToUpper() == "PRODUCTIONHEALTHCHECK")
                {

                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                }
            var bookingDetails = entitiDetails;
            if (entitiDetails.Intent == "Acronym")
            {
                entitiDetails.Acronym = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
            }
            else if (entitiDetails.Intent == "Trigger_Service")
            {
                if (entitiDetails.Project == null)
                    entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();//(string)stepContext.Result;
                if (entitiDetails.Tag == null && entitiDetails.Project.ToUpper() != "PRODUCTIONHEALTHCHECK")
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
             new PromptOptions
             {
                 Prompt = MessageFactory.Text("Please select the test"),
                 Choices = ChoiceFactory.ToChoices(new List<string> { "Smoke", "Regression" }),
                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
             }, cancellationToken);

                }
                else
                {
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                }

            }
            else
            {
                if (entitiDetails.Environment == null)
                    entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                if (entitiDetails.Project == "App-Deployment")
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter war to deploy the build.\n\n" + " Ex:Ipp-Portal:<version>,Loginservice:<version>,Client-Profile:<version>") }, cancellationToken);
                else
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter Sql script path to deploy") }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> DbInstanceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Build_Deployment":
                    if (entitiDetails.Project == "DB-Deployment")
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter DB Instance name") }, cancellationToken);
                    }
                    else
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                case "Trigger_Service":
                    if (entitiDetails.Tag == null && entitiDetails.Project.ToUpper() != "PRODUCTIONHEALTHCHECK")
                        entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            string msg = string.Empty;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    if (!string.IsNullOrEmpty(entitiDetails.Acronym))
                    {
                        entitiDetails.Acronym = (string)stepContext.Result;
                        return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word/Execute a service") }, cancellationToken);
                    }
                case "Trigger_Service":
                    if (entitiDetails.Project.ToUpper() == "PRODUCTIONHEALTHCHECK")
                    {
                        msg = $"Please confirm, Do you want to execute {entitiDetails.Portfolio} {" "}  {entitiDetails.Project} ?";
                        entitiDetails.Tag = "PRODSmokeHealthCheck";
                        if (entitiDetails.Portfolio.ToUpper() == "RAPID")
                            entitiDetails.Project = "RetrievalManagement";
                        else if (entitiDetails.Portfolio.ToUpper() == "CCV")
                        {
                            entitiDetails.Project = "CCVPROD";
                            entitiDetails.Tag = "PRODCITSmoke";
                        }
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else if (entitiDetails.Tag == "AutoDerivation")
                    {
                        entitiDetails.Tag = entitiDetails.Environment + entitiDetails.Tag;
                    }
                    if (entitiDetails.Tag == null)
                        entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();//(string)stepContext.Result;
                    msg = $"Please confirm, Do you want to execute  {entitiDetails.Tag} for {entitiDetails.Project} ?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                case "Build_Deployment":
                    if (entitiDetails.Project == "App-Deployment")
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        msg = $"Please confirm, Do you want to Deploy build for the war {entitiDetails.Buildwar} to {entitiDetails.Environment} environment ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else
                    {
                        if (entitiDetails.DbInstance == null)
                            entitiDetails.DbInstance = (string)stepContext.Result;
                        msg = $"Please confirm, Do you want to proceed with DB deployment for {entitiDetails.Buildwar} on to {entitiDetails.DbInstance} environment?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
            }
            await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you.") }, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> CaptureEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {  //var bookingDetails = await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken);
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Intent == "Build_Deployment")
            {
                if ((bool)stepContext.Result)
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive deployment status") }, cancellationToken);
                else
                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else if (entitiDetails.Intent == "Acronym")
            {
                if (!string.IsNullOrEmpty(entitiDetails.Acronym))
                {
                    entitiDetails.Acronym = (string)stepContext.Result;
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word/Execute a service") }, cancellationToken);
                }
            }
            else if (entitiDetails.Intent == "Trigger_Service")
            {
                if ((bool)stepContext.Result)
                {
                    if (entitiDetails.Tag == entitiDetails.Environment + "AutoDerivation")
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive auto derivation status") }, cancellationToken);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive test results") }, cancellationToken);
                }
                else

                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
                return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;

            if (entitiDetails.Intent == "Trigger_Service" || entitiDetails.Intent == "Build_Deployment")
            {

                entitiDetails.Email = (string)stepContext.Result;
                if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
                {
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await CaptureEmailStepAsync(stepContext, cancellationToken);
                }
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Intent == "Acronym")
            {
                //entitiDetails.Email = (string)stepContext.Result;
                /*  if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
                  {
                      stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                      return await CaptureEmailStepAsync(stepContext, cancellationToken);
                  }*/
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);

            }
            else
            {
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
            }
        }
        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }

        private IList<Choice> GetProjectChoices(string strPortFolio)
        {
            switch (strPortFolio.ToUpper())
            {
                case "PCA":
                    var cardOptions = new List<Choice>()
            {
                new Choice() { Value = "CPW", Synonyms = new List<string>() { "CPW" } },
                new Choice() { Value = "InterpretiveUpdate", Synonyms = new List<string>() { "Interpretive Update","IU" } },
                new Choice() { Value = "CPTICDLinks", Synonyms = new List<string>() { "CPTICD Links" } },
                new Choice() { Value = "ClientProfile", Synonyms = new List<string>() { "Client Profile","CP" } },
                new Choice() { Value = "Medicaid", Synonyms = new List<string>() { "Medicaid" } },
                new Choice() { Value = "ImpactAnalysis", Synonyms = new List<string>() { "Impact Analysis" } },
                new Choice() { Value = "ClientInquiry", Synonyms = new List<string>() { "Client Inquiry" } },
                new Choice() { Value = "CTA", Synonyms = new List<string>() { "CTA" } },
                new Choice() { Value = "PresentationManager", Synonyms = new List<string>() { "Presentation Manager" } },
                new Choice() { Value = "ICD-IU", Synonyms = new List<string>() { "ICDIU","icd","icd-iu","icdiu" } },
                 new Choice() { Value = "ClientApps", Synonyms = new List<string>() { "Client apps","client facing apps"} },
            };
                    return cardOptions;


                case "RAPID":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "Retrieval Management", Synonyms = new List<string>() { "RMS" } },

            };
                    return cardOptions;
                case "CCV":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "Config_Tool", Synonyms = new List<string>() { "Config Tool" } },
                new Choice() { Value = "CCV-CIT", Synonyms = new List<string>() { "CCV CIT", "CCV ISAI" } },

            };
                    return cardOptions;
                case "BUILD_DEPLOYMENT":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "App-Deployment", Synonyms = new List<string>() { "App-Deployment" } },
                 new Choice() { Value = "DB-Deployment", Synonyms = new List<string>() { "DB Deployment" } },
            };
                    return cardOptions;
                default:
                    return null;
            }
        }
    }
}
