namespace Bloop.Core;

public class Validator
{
    public static List<Error> Validate(Config config)
    {
        var failures = new List<Error>();
        BodyAndFormSet(failures, config);
        ContentTypeNoBody(failures, config);
        VariableNotDefined(failures, config);
        VariablePropsMakeSense(failures, config);
        return failures;
    }

    static void BodyAndFormSet(List<Error> errors, Config config)
    {
        foreach (var request in config.Requests)
        {
            if (request.Body != null && request.Form != null)
            {
                errors.Add(new Error($"both `body` and `form` are set on request `{request.Name}`"));
            }
        }
    }

    static void ContentTypeNoBody(List<Error> errors, Config config)
    {
        foreach (var request in config.Requests)
        {
            if (request.ContentType != null && request.Body == null)
            {
                errors.Add(new Error($"`content_type` is `{request.ContentType}` on request `{request.Name}` but `body` is missing"));
            }
        }
    }

    static void VariableNotDefined(List<Error> errors, Config config)
    {
        foreach (var request in config.Requests)
        {
            var variables = VariableHandler.GetVariables(config.Defaults, request);
            foreach (var variable in variables)
            {
                if (!config.Variables.Any(v => v.Name == variable))
                {
                    errors.Add(new Error($"variable ${{{variable}}} is used in `{request.Name}` but is not defined as a variable"));
                }
            }
        }
    }

    static void VariablePropsMakeSense(List<Error> errors, Config config)
    {
        foreach (var variable in config.Variables)
        {
            var hasValue = variable.Value != null;
            var hasJpath = variable.Jpath != null;
            var hasSource = variable.Source != null;
            var hasCommand = variable.Command != null;
            var hasCommandArgs = variable.CommandArgs != null;
            var hasFile = variable.File != null;
            var hasEnv = variable.Env != null;

            if (hasJpath && !hasSource)
            {
                errors.Add(new Error($"variable `{variable.Name}` has a `jpath` set, but is missing a `source`"));
            }

            if (!hasJpath && hasSource)
            {
                errors.Add(new Error($"variable `{variable.Name}` has a `source` set, but is missing a `jpath`"));
            }

            if (hasCommandArgs && !hasCommand)
            {
                errors.Add(new Error($"variable `{variable.Name}` has `command_args` set, but is missing a `command`"));
            }
        }
    }
}
