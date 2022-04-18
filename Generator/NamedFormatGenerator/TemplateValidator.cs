// (c) gfoidl, all rights reserved

namespace Generator.NamedFormatGenerator;

internal static class TemplateValidator
{
    public static bool ValidateTemplate(string template, out int noOfHoles)
    {
        int bracketsCount  = 0;
        int localNoOfHoles = 0;

        for (int i = 0; i < template.Length; ++i)
        {
            if (template[i] == '{')
            {
                if (bracketsCount > 0)
                {
                    break;
                }
                bracketsCount++;
                localNoOfHoles++;
            }

            if (template[i] == '}')
            {
                if (bracketsCount != 1)
                {
                    break;
                }
                bracketsCount--;
            }
        }

        noOfHoles = localNoOfHoles;
        return bracketsCount == 0;
    }
}
