// using NeuroSdk.Actions;
// using NeuroSdk.Json;
// using NeuroSdk.Websocket;


// namespace NeuroFTK
// {
//     public class NeuroTestAction : NeuroAction
//     {
//         public override string Name => "neuro test action 1";

//         protected override string Description => "this is a test";

//         protected override JsonSchema Schema => GenerateSchema();

//         protected override void Execute()
//         {
//             throw new System.NotImplementedException();
//         }

//         protected override ExecutionResult Validate(ActionJData actionData)
//         {
//             throw new System.NotImplementedException();
//         }


//         private JsonSchema GenerateSchema()
//         {
//             return null;
//             // Dictionary<string, JsonSchema> test = [];
//             // test["test"] = QJS.Enum(["1", "2", "3"]);
//             // return QJS.WrapObject(test);
//             // return test;
//             // return QJS.WrapObject(new IReadOnlyDictionary<string, JsonSchema>
//             // {
//             //     ["test"] = QJS.Enum(["1", "2", "3"])
//             // });
//         }
//     }
// }
