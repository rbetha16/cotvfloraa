// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{


    public class BookingDialog : CancelAndHelpDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public BookingDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(BookingDialog))
        {
            Configuration = configuration;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                PortfolioStepAsync,
                TagStepAsync,
                ConfirmStepAsync,
                CaptureEmailStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (EntitiDetails)stepContext.Options;
            if (bookingDetails.Intent == "Acronym")
            {
                if (bookingDetails.Acronym == null && bookingDetails.Score > 0.3)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Which acronym details would you like to have?") }, cancellationToken);
                }
                if (bookingDetails.Score < 0.3 && bookingDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word") }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(bookingDetails.Acronym, cancellationToken);
                }
            }
            else if (bookingDetails.Intent == "Trigger_Service" || bookingDetails.Intent == "Build_Deployment")
            {

                if (bookingDetails.Project == null && bookingDetails.Score > 0.3)
                {
                    string msg = string.Empty;
                    if (bookingDetails.Returnmsg == "Return" || stepContext.Result != null)
                    {
                        bookingDetails.Returnmsg = string.Empty;
                        msg = "Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number.";
                    }
                    else
                        msg = "Type any of the portfolio below";
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(Environment.NewLine + msg + Environment.NewLine + "1.PCA" + Environment.NewLine + "2.CCV" + Environment.NewLine + "3.Rapid") }, cancellationToken);
                }
                else if (bookingDetails.Score < 0.3 && bookingDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other Project") }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(bookingDetails.Project, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(bookingDetails.Project, cancellationToken);
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
                case "Build_Deployment":

                    if (entitiDetails.Project == null && entitiDetails.Score > 0.3)
                    {
                        string msg = string.Empty;
                        if (entitiDetails.Returnmsg == "Return")
                        {
                            entitiDetails.Returnmsg = string.Empty;
                            msg = "Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number.";
                        }
                        else
                            msg = "Type any of the projects below";
                        entitiDetails.Portfolio = stepContext.Result.ToString();
                        string[] portfolioArray = { "PCA", "CCV", "RAPID" };
                        bool isNum = int.TryParse(stepContext.Result.ToString(), out int num);
                        if (!isNum && !portfolioArray.Any(x => x == stepContext.Result.ToString().ToUpper()))
                        {
                            entitiDetails.Returnmsg = "Return";
                            entitiDetails.Portfolio = null;
                            stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                            return await DestinationStepAsync(stepContext, cancellationToken);

                        }
                        else
                        {
                            if (entitiDetails.Portfolio == null)
                                entitiDetails.Portfolio = (string)stepContext.Result;
                            if (!string.IsNullOrEmpty(entitiDetails.Portfolio))
                            {

                                if (entitiDetails.Portfolio == "1")
                                    entitiDetails.Portfolio = "PCA";
                                else if (entitiDetails.Portfolio == "2")
                                    entitiDetails.Portfolio = "CCV";
                                else if (entitiDetails.Portfolio == "3")
                                    entitiDetails.Portfolio = "Rapid";

                            }
                            if (portfolioArray.Any(x => x == entitiDetails.Portfolio.ToUpper()))
                            {
                                if (entitiDetails.Portfolio.ToUpper() == "PCA")
                                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                                    {

                                        Prompt = MessageFactory.Text(Environment.NewLine + msg + Environment.NewLine + "1.CPW" + Environment.NewLine + "2.InterpretiveUpdate" + Environment.NewLine + "3.CPTICDLinks" + Environment.NewLine + "4.ClientProfile" + Environment.NewLine + "5.Medicaid" + Environment.NewLine + "6.CPQ" + Environment.NewLine + "7.ImpactAnalysis" + Environment.NewLine + "8.ClientInquiry" + Environment.NewLine + "9. CTA" + Environment.NewLine + "10. PresentationManager")
                                    }, cancellationToken);
                                else if (entitiDetails.Portfolio.ToUpper() == "CCV")
                                {
                                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Still we are working on to integrate the CCV projects with floraa. Please try for PCA projects"), cancellationToken);
                                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Still we are working on to integrate the Rapid projects with floraa. Please try for PCA projects"), cancellationToken);
                                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                                }
                            }
                            else {
                                entitiDetails.Returnmsg = "Return";
                                entitiDetails.Portfolio = null;
                                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                                return await DestinationStepAsync(stepContext, cancellationToken);
                            }
                        }
                    }
                    else if (entitiDetails.Score < 0.3 && entitiDetails.Acronym == null)
                    {
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other Project") }, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
                    }
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
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            string[] projectsArray = { "CPW", "INTERPRETIVEUPDATE", "CPTICDLINKS", "CLIENTPROFILE", "MEDICAID", "CPQ", "IMPACTANALYSIS", "CLIENTINQUIRY", "CTA", "PRESENTATIONMANAGER", "PRODUCTIONHEALTHCHECK" };
            bool isNum = int.TryParse(stepContext.Result.ToString(), out int num);
            if (!isNum && !projectsArray.Any(x => x == stepContext.Result.ToString().ToUpper()) && entitiDetails.Intent == "Trigger_Service")
            {
                entitiDetails = stepContext.Result != null
                        ?
                    await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                        :
                    new EntitiDetails();
                if (entitiDetails.Intent == "Acronym")
                {

                    entitiDetails.Returnmsg = "Return";
                    entitiDetails.Project = null;
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await PortfolioStepAsync(stepContext, cancellationToken);
                }
            }
            var bookingDetails = entitiDetails;
            if (entitiDetails.Intent == "Acronym")
            {
                entitiDetails.Acronym = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
            }
            else
            {
                if (entitiDetails.Project == null)
                    entitiDetails.Project = (string)stepContext.Result;
                //string[] projectsArray = { "CPW", "INTERPRETIVEUPDATE", "CPTICDLINKS", "CLIENTPROFILE", "MEDICAID", "CPQ", "IMPACTANALYSIS", "CLIENTINQUIRY", "CTA", "PRESENTATIONMANAGER" };
                if (!string.IsNullOrEmpty(bookingDetails.Project))
                {

                    if (entitiDetails.Project == "1")
                        entitiDetails.Project = "CPW";
                    else if (entitiDetails.Project == "2")
                        entitiDetails.Project = "InterpretiveUpdate";
                    else if (entitiDetails.Project == "3")
                        entitiDetails.Project = "CPTICDLinks";
                    else if (entitiDetails.Project == "4")
                        entitiDetails.Project = "ClientProfile";
                    else if (entitiDetails.Project == "5")
                        entitiDetails.Project = "Medicaid";
                    else if (entitiDetails.Project == "6")
                        entitiDetails.Project = "CPQ";
                    else if (entitiDetails.Project == "7")
                        entitiDetails.Project = "ImpactAnalysis";
                    else if (entitiDetails.Project == "8")
                        entitiDetails.Project = "ClientInquiry";
                    else if (entitiDetails.Project == "9")
                        entitiDetails.Project = "CTA";
                    else if (entitiDetails.Project == "10")
                        entitiDetails.Project = "PresentationManager";
                }
                if (projectsArray.Any(x => x == entitiDetails.Project.ToUpper()))
                {
                    if ((entitiDetails.Tag == null || entitiDetails.Buildwar == null) && entitiDetails.Project.ToUpper() != "PRODUCTIONHEALTHCHECK")
                    {
                        string msg = string.Empty;
                        if (entitiDetails.Returnmsg == "Return")
                        {
                            entitiDetails.Returnmsg = string.Empty;
                            msg = "Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number.";
                        }
                        else
                            msg = "Type any of the Tests below";
                        if (entitiDetails.Intent == "Build_Deployment" && entitiDetails.Buildwar == null)
                            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter war to deploy the build") }, cancellationToken);

                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(Environment.NewLine + msg + Environment.NewLine + "1.Smoke" + Environment.NewLine + "2.Regression") }, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.ReplaceDialogAsync("WaterfallDialog", entitiDetails, cancellationToken);
                        //return await stepContext.NextAsync(bookingDetails, cancellationToken);
                    }
                }
                else
                {
                    entitiDetails.Returnmsg = "Return";
                    entitiDetails.Project = null;
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await PortfolioStepAsync(stepContext, cancellationToken);
                }
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
                        entitiDetails.Tag = "PRODSmokeHealthCheck";
                        msg = $"Please confirm, Do you want to execute  {entitiDetails.Project} ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    if (entitiDetails.Tag == null)
                        entitiDetails.Tag = (string)stepContext.Result;
                    if (!string.IsNullOrEmpty(entitiDetails.Tag))
                    {
                        if (entitiDetails.Tag == "1")
                            entitiDetails.Tag = "Smoke";
                        else if (entitiDetails.Tag == "2")
                            entitiDetails.Tag = "Regression";
                    }
                    if (entitiDetails.Tag.ToUpper() == "SMOKE" || entitiDetails.Tag.ToUpper() == "REGRESSION" || entitiDetails.Project.ToUpper() == "PRODUCTIONHEALTHCHECK")
                    {
                        msg = $"Please confirm, Do you want to execute  {entitiDetails.Tag} tests for {entitiDetails.Project} ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);

                    }
                    else
                    {
                        entitiDetails.Returnmsg = "Return";
                        entitiDetails.Tag = null;
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await TagStepAsync(stepContext, cancellationToken);

                    }
                case "Build_Deployment":
                    if (entitiDetails.Buildwar == null)
                        entitiDetails.Buildwar = (string)stepContext.Result;
                    msg = $"Please confirm, Do you want to Deploy  {entitiDetails.Project} buil for the war {entitiDetails.Buildwar} ?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
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
                    return await stepContext.NextAsync(entitiDetails.Buildwar, cancellationToken);
                else
                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (entitiDetails.Intent == "Acronym")
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
                if (entitiDetails.Email != null)
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive test results") }, cancellationToken);
                else if ((bool)stepContext.Result)
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive test results") }, cancellationToken);

                else

                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
                return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;

            if (entitiDetails.Intent == "Trigger_Service")
            {

                entitiDetails.Email = (string)stepContext.Result;
                if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
                {
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await CaptureEmailStepAsync(stepContext, cancellationToken);
                }
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Intent == "Acronym" || entitiDetails.Intent == "Build_Deployment")
            {
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
    }
}
