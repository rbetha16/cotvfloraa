// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.Bot.Schema;
using CoreBot.Dialogs;
using CoreBot.Helpers;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.AI.QnA;
using System.Net.Http;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {

        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private bool isContinue = false;

        //private IHostingEnvironment _hostingEnvironment;
        private string strRole = string.Empty;
        private string strUser = string.Empty;
        private string strIntent = string.Empty;
        private string strProject = string.Empty;
        private string strJenkinUrl = string.Empty;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger, IHttpClientFactory httpClientFactory)//
            : base(nameof(MainDialog))
        {
            Configuration = configuration;
            Logger = logger;
            _httpClientFactory = httpClientFactory;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ActionDialog(configuration, logger));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
                ConfirmStepAsync,
                FeedbackStepAsync,

            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(Configuration["LuisAppId"]) || string.IsNullOrEmpty(Configuration["LuisAPIKey"]) || string.IsNullOrEmpty(Configuration["LuisAPIHostName"]))
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file. "), cancellationToken);

                    return await stepContext.NextAsync(null, cancellationToken);
                }
                else
                {
                    strUser = stepContext.Context.Activity.From.Name;
                    string strAaId = stepContext.Context.Activity.From.AadObjectId;//"6701e37f-35a7-4026-9153-1599a9baf625"; //"96ea879d-c36d-4444-a66e-0fdff56e1cd3";                
                    string[] userName = new string[2];
                    if (!string.IsNullOrEmpty(strUser))
                        userName = strUser.Split(" ");
                    else
                        userName[0] = "User";
                    strRole = getUserRole(strAaId);
                    if (userName.Length > 1)
                        strUser = userName[1];
                    else
                        strUser = userName[0];

                    if (strRole.ToUpper().Contains("OTHER"))
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Hi " + strUser + "\nSorry!! You dont have access to floraa actions. Please contact administrator."), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                    string strMsg = string.Empty;
                    var entitiDetails = (EntitiDetails)stepContext.Options;
                    if (entitiDetails == null || entitiDetails.Returnmsg == "returnQuit")
                        strMsg = getRoleBasedMessage(strRole, isContinue, strUser);
                    else if (entitiDetails.Returnmsg == "return")
                        strMsg = "What more you want to know?";
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(strMsg), Style = ListStyle.List }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(ex.ToString()) }, cancellationToken);

            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Call LUIS and gather any potential action details. (Note the TurnContext has the response to the prompt.)
            var entitiDetails = stepContext.Result != null
                      ?
                  await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                      :
                  new EntitiDetails();
            var eDetails = (EntitiDetails)entitiDetails;
            bool isNum = int.TryParse(stepContext.Result.ToString().Trim(), out int num);
            if (strRole.ToUpper().Equals("ADMIN"))
            {
                if (isNum)
                {
                    if (num > 5)
                    {
                        isContinue = true;
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await IntroStepAsync(stepContext, cancellationToken);
                    }
                    if (stepContext.Result.ToString() == "1")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "2")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Build_Deployment";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "3")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDtls.Project = "ProductionHealthCheck";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "4")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "UsefulLinks";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "5")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDtls.Project = "ClientProfile";
                        eDtls.Tag = "AutoDerivation";
                        eDetails = eDtls;
                    }
                }
                //else if (eDetails.Intent == "Acronym" && (string.IsNullOrEmpty(eDetails.Acronym) && eDetails.Score < 0.7))
                //{
                //    isContinue = true;
                //    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                //    return await IntroStepAsync(stepContext, cancellationToken);
                //}
            }
            else if (strRole.ToUpper().Equals("CP-ADMIN"))
            {
                if (isNum)
                {
                    if (num > 4)
                    {
                        isContinue = true;
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await IntroStepAsync(stepContext, cancellationToken);
                    }
                    if (stepContext.Result.ToString() == "1")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "2")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Build_Deployment";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "3")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDtls.Project = "ProductionHealthCheck";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "4")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "UsefulLinks";
                        eDetails = eDtls;
                    }                    
                }
                else if (eDetails.Intent == "Acronym" && (string.IsNullOrEmpty(eDetails.Acronym) && eDetails.Score < 0.7))
                {
                    isContinue = true;
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await IntroStepAsync(stepContext, cancellationToken);
                }
                else if (eDetails.Intent == "Build_Deployment")
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. You dont have access to Build deployments. Please contact administrator."), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            else if (strRole.ToUpper().Equals("USER"))
            {
                if (isNum)
                {
                    if (num > 3)
                    {
                        isContinue = true;
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await IntroStepAsync(stepContext, cancellationToken);
                    }
                    if (stepContext.Result.ToString() == "1")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "2")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Trigger_Service";
                        eDtls.Project = "ProductionHealthCheck";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "3")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "UsefulLinks";
                        eDetails = eDtls;
                    }
                }
                else if (eDetails.Intent == "Acronym" && (string.IsNullOrEmpty(eDetails.Acronym) && eDetails.Score < 0.7))
                {
                    isContinue = true;
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await IntroStepAsync(stepContext, cancellationToken);
                }
                else if (eDetails.Intent == "Build_Deployment")
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. You dont have access to Build deployments. Please contact administrator."), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            else if (strRole.ToUpper().Equals("DEVOPS"))
            {
                if (isNum)
                {
                    if (num > 2)
                    {
                        isContinue = true;
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await IntroStepAsync(stepContext, cancellationToken);
                    }
                    if (stepContext.Result.ToString() == "1")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "Build_Deployment";
                        eDetails = eDtls;
                    }
                    else if (stepContext.Result.ToString() == "2")
                    {
                        EntitiDetails eDtls = new EntitiDetails();
                        eDtls.Score = 0.9;
                        eDtls.Intent = "UsefulLinks";
                        eDetails = eDtls;
                    }
                }
                else if (eDetails.Intent == "Acronym" && (string.IsNullOrEmpty(eDetails.Acronym) && eDetails.Score < 0.7))
                {
                    isContinue = true;
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await IntroStepAsync(stepContext, cancellationToken);
                }
                else if (eDetails.Intent == "Trigger_Service")
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. You dont have access to Test Execution. Please contact administrator."), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            if (eDetails.Intent == "Trigger_Service")
            {
                if (!string.IsNullOrEmpty(entitiDetails.Project) && entitiDetails.Project == "AutoDerivation")
                {
                    entitiDetails.Project = "ClientProfile";
                    entitiDetails.Tag = "AutoDerivation";
                }
                strJenkinUrl = Configuration["JenkinsURL1"];
                if (Configuration["CheckJenkins"] == "true")
                    strJenkinUrl = CheckJenkinsServer();
                if (string.IsNullOrEmpty(strJenkinUrl) || Configuration["IsJenkinsDown"] == "true")
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(Configuration["MaintainanceMessage"]), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            else if (eDetails.Intent == "UsefulLinks")
            {

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Click on the link: [Useful Links](https://floraafeedback.z13.web.core.windows.net/CotivitiUsefulLink.html)"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            if ((string.IsNullOrEmpty(eDetails.Intent) || eDetails.Score < 0.3))
            {
                var httpClient = _httpClientFactory.CreateClient();

                var qnaMaker = new QnAMaker(new QnAMakerEndpoint
                {
                    KnowledgeBaseId = Configuration["QnAKnowledgebaseId"],
                    EndpointKey = Configuration["QnAEndpointKey"],
                    Host = Configuration["QnAEndpointHostName"]
                },
                null,
                httpClient);
                EntitiDetails entitiDetails1 = new EntitiDetails();
                if (stepContext.Result.ToString().ToUpper().Trim() == "QUIT")
                {
                    entitiDetails1.Returnmsg = "returnQuit";
                    return await stepContext.BeginDialogAsync(nameof(MainDialog), entitiDetails1, cancellationToken);
                }
                else
                    entitiDetails1.Returnmsg = "return";
                var response = await qnaMaker.GetAnswersAsync(stepContext.Context);
                if (response != null && response.Length > 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(MainDialog), entitiDetails1, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. I didn't get you. Please select appropriate action"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            return await stepContext.BeginDialogAsync(nameof(ActionDialog), eDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                if (stepContext.Result != null)
                {
                    var entitiDetails = (EntitiDetails)stepContext.Result;
                    strIntent = entitiDetails.Intent;
                    switch (entitiDetails.Intent)
                    {
                        case "Acronym":
                            // If the child dialog ("ActionDialog") was cancelled or the user failed to confirm, the Result here will be null.                        
                            var result = (EntitiDetails)stepContext.Result;
                            var msg = string.Empty;
                            // Now we have all the Action details call the action service.
                            string URL = "https://floraa-acronymapi.azurewebsites.net/api/values/GetAcronym?id=" + result.Acronym;
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
                            req.Method = "GET";
                            req.Headers.Add("X-ApiKey", "6b0f60c2-40ef-43d5-89ef-905e048d610b:a99b5fcb-064f-433a-afde-e0e46f441005");
                            req.Accept = "text/json";
                            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                            {
                                if (resp.StatusCode == HttpStatusCode.OK)
                                {
                                    StreamReader reader = new StreamReader(resp.GetResponseStream());
                                    string strResult = reader.ReadToEnd();
                                    if (strResult.Length > 2)
                                        msg = strResult;
                                    else
                                        msg = $"The Acronym of {result.Acronym} not found please try other word";
                                }
                                else
                                    msg = $"The Acronym of {result.Acronym} not found please try other word";
                            }
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                            msg = "Thank you " + getUserName(stepContext) + ". Did I take care of your request?";
                            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                        case "Trigger_Service":

                            string strTag = "";
                            if (entitiDetails.Tag.ToUpper() == "SMOKE")
                            {
                                strTag = entitiDetails.Tag;
                                entitiDetails.Tag = "QASmoke";
                            }
                            else if (entitiDetails.Tag.ToUpper() == "REGRESSION")
                            {
                                strTag = "Regression";
                                entitiDetails.Tag = "Sanity";

                                JenkinsService jenkinsService = new JenkinsService();
                                var lastBuild = jenkinsService.getLastBuildStatus(entitiDetails.Project).Result;
                                var lastBuildType = lastBuild["actions"];

                                if (lastBuild != null)
                                {
                                    var x = lastBuildType[0]["parameters"][0];
                                    var sTagType = lastBuildType[0]["parameters"][0]["value"];
                                    //   var a1 = lastBuildType["_class"]["parameter"];

                                    var lastBuildStatus = lastBuild["result"].ToString();
                                    if (string.IsNullOrEmpty(lastBuildStatus) || lastBuildStatus == "{}")
                                    {

                                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Same user or some other user has already triggerd this project test execution please try after some time."), cancellationToken);
                                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                                    }
                                    else if (sTagType == "@Sanity" || sTagType == "@Regression")
                                    {
                                        var lastBuildTime = (long)lastBuild["timestamp"];

                                        var timeStamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

                                        var timeDiference = (timeStamp - lastBuildTime);

                                        var timeMinutes = timeDiference / (1000 * 60);
                                        if (timeMinutes <= 1200)
                                        {
                                            var url = lastBuild["url"].ToString() + "Serenity_20Report/";
                                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("This is already executed in last one hour click on the link to view results: [Regression Results](" + url + ")"), cancellationToken);
                                            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

                                        }
                                    }
                                }
                            }


                            strProject = entitiDetails.Project + "-" + entitiDetails.Tag;
                            string sPOSTURL = strJenkinUrl + "/job/" + entitiDetails.Project + "/buildWithParameters?TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa";
                            HttpWebRequest requestObjPost = (HttpWebRequest)HttpWebRequest.Create(sPOSTURL);
                            requestObjPost.Method = "POST";
                            requestObjPost.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                            requestObjPost.ContentType = "application/json";
                            requestObjPost.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                            requestObjPost.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
                            using (var streamWriter = new StreamWriter(requestObjPost.GetRequestStream()))
                            {
                                var httpResponse = (HttpWebResponse)requestObjPost.GetResponse();
                                if (httpResponse.StatusCode == HttpStatusCode.Created)
                                {
                                    if (entitiDetails.Tag.ToUpper() == "AUTODERIVATION")
                                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Triggered " + entitiDetails.Tag + " for " + entitiDetails.Project + ". The results will be sent to your email shortly."), cancellationToken);
                                    else

                                     await stepContext.Context.SendActivityAsync(MessageFactory.Text("Triggered " + strTag + " for " + entitiDetails.Project + ". The results will be sent to your email shortly. \n You can also see the live execution in below URL: [Click Here](http://usddccntr04:8080/) "), cancellationToken);

                                    if (entitiDetails.Tag == "Sanity")
                                    {

                                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Estimated time to complete the execution 30-45 mins"), cancellationToken);
                                    }

                                }
                            }

                            isContinue = false;
                            msg = "Thank you " + getUserName(stepContext) + ". Did I take care of your request?";
                            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);

                        case "Build_Deployment":
                            strProject = entitiDetails.Project;
                            string buildURL = string.Empty;
                            if (strProject == "App-Deployment")
                                buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/floraa_qadeployer/buildWithParameters?token=floradeploy&Floraa_Intent=" + entitiDetails.Buildwar + "&Email=" + entitiDetails.Email+"&Environment="+entitiDetails.Environment.ToLower();
                            else if (strProject == "DB-Deployment")
                                buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/PCA_Sql_Runner/buildWithParameters?Sqlpath=" + entitiDetails.Buildwar + "&DBInstance=" + entitiDetails.DbInstance + "&EmailRecipients=" + entitiDetails.Email;
                            HttpWebRequest reqObj = (HttpWebRequest)HttpWebRequest.Create(buildURL);
                            reqObj.Method = "POST";
                            //reqObj.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                            reqObj.ContentType = "application/json";
                            reqObj.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                            reqObj.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
                            using (var streamWriter = new StreamWriter(reqObj.GetRequestStream()))
                            {
                                var httpResponse = (HttpWebResponse)reqObj.GetResponse();
                                if (httpResponse.StatusCode == HttpStatusCode.Created)
                                {
                                    if (strProject == "App-Deployment")
                                        msg = "App Deployment with war " + entitiDetails.Buildwar + " to "+entitiDetails.Environment+ " is initiated. you will receive the email shortly.";
                                    else
                                        msg = entitiDetails.Project + " with " + entitiDetails.Buildwar + " script  on to " + entitiDetails.DbInstance + " is initiated. you will receive the email shortly.";
                                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                                }
                            }
                            isContinue = false;
                            msg = "Thank you " + getUserName(stepContext) + ". Did I take care of your request?";
                            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                        default:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry.. I didn't get you Please Try again.\nThank you"), cancellationToken);
                            isContinue = false;
                            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }

                }
                else
                {
                    isContinue = false;

                    var msg = "Thank you " + getUserName(stepContext);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Error occured while running service. Please try agian\nThank you " + getUserName(stepContext) + ".\n" + ex), cancellationToken);
                throw;
            }

        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                if ((bool)stepContext.Result)
                {
                    FeedbackEntity feedbackEntity = new FeedbackEntity();
                    feedbackEntity.PartitionKey = stepContext.Context.Activity.From.Name;
                    feedbackEntity.RowKey = Guid.NewGuid().ToString();
                    feedbackEntity.Role = getUserRole(stepContext.Context.Activity.From.Name);
                    feedbackEntity.Status = stepContext.Result.ToString();
                    feedbackEntity.FeedBack = "The request has fulfilled";
                    feedbackEntity.Intent = strIntent;
                    feedbackEntity.Project = strProject;

                    StorageHelper storageHelper = new StorageHelper();
                    await storageHelper.StoreFeedback(Configuration, feedbackEntity);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your feedback"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please tell me what went wrong") }, cancellationToken);
                }
            }
            catch (Exception)
            {

                throw;
            }


        }

        private async Task<DialogTurnResult> FeedbackStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FeedbackEntity feedbackEntity = new FeedbackEntity();
            feedbackEntity.PartitionKey = stepContext.Context.Activity.From.Name;
            feedbackEntity.RowKey = Guid.NewGuid().ToString();
            feedbackEntity.Role = getUserRole(stepContext.Context.Activity.From.Id);
            feedbackEntity.Status = "False";
            feedbackEntity.FeedBack = stepContext.Result.ToString();
            feedbackEntity.Intent = strIntent;
            feedbackEntity.Project = strProject;

            StorageHelper storageHelper = new StorageHelper();
            await storageHelper.StoreFeedback(Configuration, feedbackEntity);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your feedback"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private string getRoleBasedMessage(string UserRole, bool contFlag, string userName)
        {
            string strMsg = string.Empty;
            if (UserRole.ToUpper().Equals("ADMIN"))
                strMsg = "Hi " + userName + ". Please Type any action below\n\n" + "1. Test Execution\n\n" + "2. Deploy build\n\n" + "3. Execute Production Health Check\n\n" + "4. Useful Links\n\n" + "5. Auto Derivation\n\n" + "You can do much more.. Type 'help' to see what more I can do";
            else if (UserRole.ToUpper().Equals("USER"))
                strMsg = "Hi " + userName + ". Please Type any action below\n\n" + "1. Test Execution\n\n" + "2. Execute Production Health Check\n\n" + "3. Useful Links\n\n" + "You can do much more.. Type 'help' to see what more I can do";
            else if (UserRole.ToUpper().Equals("CP-ADMIN"))
                strMsg = "Hi " + userName + ". Please Type any action below\n\n" + "1. Test Execution\n\n" + "2. Deploy build\n\n" + "3. Execute Production Health Check\n\n" + "4. Useful Links\n\n" + "You can do much more.. Type 'help' to see what more I can do";
            else if (UserRole.ToUpper().Equals("DEVOPS"))
                strMsg = "Hi " + userName + ". Please Type any action below\n\n" + "1. Deploy build\n\n" + "2. Useful Links\n\n" + "You can do much more.. Type 'help' to see what more I can do";
            else
                strMsg = "Hi " + userName + "!! \nSorry!! You dont have access to floraa actions. Please contact administrator.";
            if (isContinue)
                strMsg = strMsg.Replace("Hi " + userName + ". " + Environment.NewLine + "Please Type any action below", "Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number.");
            isContinue = false;
            return strMsg;
        }

        private string getUserRole(string strUser)
        {
            string strResult = string.Empty;
            string URL = Configuration["UserRolesAPI"] + "/api/values/GetUser?AaId=" + strUser;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
            req.Method = "GET";
            req.Headers.Add("X-ApiKey", "6b0f60c2-40ef-43d5-89ef-905e048d610b:a99b5fcb-064f-433a-afde-e0e46f441005");
            req.Accept = "text/json";
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader reader = new StreamReader(resp.GetResponseStream());
                    strResult = reader.ReadToEnd();
                    strResult = strResult.Replace("\"", "");
                    if (strResult.Length <= 2)
                        strResult = "Other";
                }
            }

            return strResult;

        }

        private string getUserName(WaterfallStepContext stepContext)
        {
            string user = stepContext.Context.Activity.From.Name;
            string[] userName = new string[2];
            if (!string.IsNullOrEmpty(user))
                userName = user.Split(" ");
            else
                userName[0] = "User";
            if (userName.Length > 1)
                user = userName[1];
            else return "User";
            return user;


        }

        private string CheckJenkinsServer()
        {
            HttpWebRequest request = WebRequest.Create(Configuration["JenkinsURL1"]) as HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
            request.ContentType = "application/json";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            //Getting the Web Response.
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                return Configuration["JenkinsURL1"];
            }
            catch (Exception)
            {
                try
                {
                    request = WebRequest.Create(Configuration["JenkinsURL2"]) as HttpWebRequest;
                    request.Method = "HEAD";
                    request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                    request.ContentType = "application/json";
                    request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                    request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    return Configuration["JenkinsURL2"];
                }
                catch (Exception)
                {

                    return string.Empty;
                }
            }


        }

        //private IList<Choice> GetRoleBasedMenu(string strRole)
        //{
        //    var cardOptions = new List<Choice>();
        //    if (strRole.ToUpper().Contains("ADMIN"))
        //    {
        //    cardOptions = new List<Choice>()
        //    {
        //        new Choice() { Value = "Test Exection", Synonyms = new List<string>() { "CPW" } },
        //        new Choice() { Value = "Deploy Build", Synonyms = new List<string>() { "Interpretive Update","IU" } },
        //        new Choice() { Value = "Execute Production Health Check", Synonyms = new List<string>() { "CPTICD Links" } },               
        //    };
        //    }
        //    else if (strRole.ToUpper().Contains("USER"))
        //    {
        //     cardOptions = new List<Choice>()
        //    {
        //        new Choice() { Value = "Test Exection", Synonyms = new List<string>() { "CPW" } },               
        //        new Choice() { Value = "Execute Production Health Check", Synonyms = new List<string>() { "CPTICD Links" } },
        //    };
        //    }
        //    else if (strRole.ToUpper().Contains("DEVOPS"))
        //    {
        //    cardOptions = new List<Choice>()
        //    {             
        //        new Choice() { Value = "Deploy Build", Synonyms = new List<string>() { "Interpretive Update","IU" } },               
        //    };
        //    }
        //    return cardOptions;
        //}

    }
}

