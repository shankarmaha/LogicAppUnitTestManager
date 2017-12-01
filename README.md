# LogicAppUnitTestManager

Logic App Test manager

Every developer has the best intention to test their code before they release. Most of them write unit-test cases to test their code in isolation. They bundle their unit tests to their CI/CD pipeline to get build issues at an early stage. As an integration developer, I would also like to add unit test for my Azure logic apps and add them to my CI/CD pipeline. Now, I believe if you are reading this, you know what Logic app is. So I would just skip to the Unit testing part. As of now, we don't have a Unit testing framework for Logic apps. Mike Stephenson did a session on Integration Monday and showcased how someone could write an Acceptance test for a logic app using EventHub. I liked it very much, but I wanted to use the Logic apps SDK itself to unit test the app. The SDK provides a rich framework for triggerring a logic app and getting the results of each of the actions. However, you need a SPN created on the azure active directory and a contributor role should be assigned in order to connect to the azure subscription and read the workflows.


## How to use

You have two options
1. Clone/Fork this github project. Build it and add it as a reference in your C# unit test project
2. I have packaged this project as a nuget package. You can download from nuget. PackageName: LogicAppUnitTestManager.TestFramework

### Add settings in the app.confile file

Before you do this, you need to create a SPN and set the role as Contributor. Please refer to the powershell script in the below github page

https://gist.github.com/coreyasmith/29827f5d722c724a9d4a07d65da9d1f7

Add the below app settings to your C# test project app.config. Replace the xx with the correct values from your azure subscription. 
```
<appSettings>
    <add key="SubscriptionId" value="XXXXXX" />
    <add key="TenantId" value="XXX" />
    <add key="WebApiApplicationId" value="XXX" />
    <add key="Secret" value="XXXX" />
</appSettings>
```

### Unit Test framework

You can use any unit test framework. I used SpecFlow to define my acceptance tests. Add the below code to setup the TrackingManager. 
```
 [Binding]
    public class Shared
    {
        [BeforeScenario("CleanTheSystem")]
        public void CleanTheSystem()
        {
            LogicAppManager.Clear();
        }

        [BeforeScenario("StartTrackingManager")]
        public void StartTracking()
        {
            LogicAppManager.Start();
        }

        [AfterScenario("StopTrackingManager")]
        public void StopTracking()
        {
            LogicAppManager.Clear();
            LogicAppManager.Stop();
        }
    }
```

### Sample specflow feature file

```
Feature: BankStatementSourceCS
	In order to unit test the Bank statement import functionality 
	As an integrtion developer
	I want to be validate each of the logic app actions in the Bank Statement Source CS Logic app

@CleanTheSystem
@StartTrackingManager
@StopTrackingManager
Scenario: Import valid daily file with 1 account and valid transactions
	Given the folder location is empty
		| folderName  |
		| download    |
		| archived    |
		| bad-data    |
		| failed      |
		| in-progress |
	And the logic app is enabled
	When a BAI file is copied to the download folder
	And  a trigger message is sent to the logic app - BankStatementSourceCS
	Then the logic will receive the message
	And it will list all the files in the download folder
	And it will get the file content
	And it will copy the file to the in-progress folder
	And it will delete the file from the download folder
	And it will transform the message from BAICSV to BAIXML
	And it will transform the message from BAIXMl to Reconcilation
	And it will transform the message from Reconciliation to Canonical
```

### Sample test code

```
        [Given(@"the folder location is empty")]
        public void GivenTheFolderLocationIsEmpty(Table table)
        {
            var connection = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            var storageHelper = new Utilities.AzureFileShareHelper(connection);

            foreach(var row in table.Rows)
            {
                var folderName = row[0];
                storageHelper.ClearFolder(folderName, "bankstatement");
            }
        }

        [Given(@"the logic app is enabled")]
        public void GivenTheLogicAppIsEnabled()
        {
            var enabled = LogicAppManager.CheckIfLogicAppIsEnabled("resourcegroup","logicappname");
            Assert.AreEqual(true, enabled);
        }
        
        [When(@"a BAI file is copied to the download folder")]
        public void WhenABAIFileIsCopiedToTheDownloadFolder()
        {
            var connection = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            var storageHelper = new Utilities.AzureFileShareHelper(connection);

            storageHelper.CopyFile(@"filepath", "bankstatement", "download", "TestFile.txt");
        }

        [When(@"a trigger message is sent to the logic app - BankStatementSourceCS")]
        public async Task WhenATriggerMessageIsSentToTheLogicApp_BankStatementSourceCS()
        {
            var status = await LogicAppManager.ExecuteLogicApp(LogicAppManager.TriggerType.Http, "manual", new StringContent(""));
            Assert.AreEqual(System.Net.HttpStatusCode.Accepted.ToString(), status);
        }


        [Then(@"the logic will receive the message")]
        public void ThenTheLogicWillReceiveTheMessage()
        {
            Assert.IsNotNull(LogicAppManager.RunId);
        }
        
        [Then(@"it will list all the files in the download folder")]
        public async Task ThenItWillListAllTheFilesInTheDownloadFolder()
        {
            var result = await LogicAppManager.CheckLogicAppAction("List_files");
            Assert.AreEqual("Succeeded", result); 
        }

        [Then(@"it will get the file content")]
        public async Task ThenItWillGetTheFileContent()
        {
            var result = await LogicAppManager.CheckLogicAppAction("Get_file_content");
            Assert.AreEqual("Succeeded", result);
        }

``` 
### Next Steps

I believe this is just enough for doing very basic unit testing for your logic apps. However, I think the framework could be extended to include lot of other features. If you have any comments please write to me shankar.sekar@gmail.com
