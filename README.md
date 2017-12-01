# LogicAppUnitTestManager

Logic App Test manager

Every developer has the best intention to test their code before they release. Most of them write unit-test cases to test their code in isolation. They bundle their unit tests to their CI/CD pipeline to get build issues at an early stage. As an integration developer, I would also like to add unit test for my Azure logic apps and add them to my CI/CD pipeline. Now, I believe if you are reading this, you know what Logic app is. So I would just skip to the Unit testing part. As of now, we don't have a Unit testing framework for Logic apps. Mike Stephenson did a session on Integration Monday and showcased how someone could write an Acceptance test for a logic app using EventHub. I liked it very much, but I wanted to use the Logic apps SDK itself to unit test the app. The SDK provides a rich framework for triggerring a logic app and getting the results of each of the actions. However, you need a SPN created on the azure active directory and a contributor role should be assigned in order to connect to the azure subscription and read the workflows.


