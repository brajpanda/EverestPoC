using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MARC.Everest.Attributes;
using MARC.Everest.DataTypes;
using MARC.Everest.DataTypes.Interfaces;
using MARC.Everest.Formatters.XML.Datatypes.R1;
using MARC.Everest.RMIM.UV.CDAr2.POCD_MT000040UV;
using MARC.Everest.RMIM.UV.CDAr2.Vocabulary;
using MARC.Everest.Xml;

namespace EverestPoC
{
    class Program
    {
        //TODO: 
        //1:"MakeAssignedEntity" method should be refactored. 
        //  I noticed sometimes it is the same person and sometimes it is different. 
        //  So I kept it individually for all nodes in their own method. 
        //  A BA has to confirm which all fields for assigned entities vary for which scenarios. 
        //TODO: 
        //2:Similarly "MakeObservation", "MakeAct" etc can be refactored?
        //  Evaluate when the entire code is ready.

        public static Dictionary<string, string> StaticCCDAData = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            //TODO: All static data should be read from the XML instead of being hardcoded
            StaticCCDAData = GetXMLValues();

            MakeCCDA();
        }

        public static Dictionary<string, string> GetXMLValues()
        {
            //TODO: Read from App_Data instead of bin\\debug
            string xmlFilePath = "StaticCCDAData.xml";
            Dictionary<string, string> dictionaryObject = new Dictionary<string, string>();
            try
            {
                if (File.Exists(xmlFilePath))
                {
                    XmlDocument ccdDocument = new XmlDocument();
                    ccdDocument.Load(xmlFilePath);
                    XmlNodeList xmlNodelist = ccdDocument.SelectNodes("section");
                    foreach (XmlElement node in xmlNodelist[0].ChildNodes)
                    {
                        dictionaryObject.Add(node.Attributes[0].Value, node.Attributes[1].Value);
                    }
                }
                return dictionaryObject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void MakeCCDA()
        {
            // We can speed up initial serialization by loading a cached formatter assembly
            MARC.Everest.Formatters.XML.ITS1.Formatter fmtr = new MARC.Everest.Formatters.XML.ITS1.Formatter();
            fmtr.GraphAides.Add(new DatatypeFormatter());
            fmtr.ValidateConformance = false;

            ClinicalDocument ccda = new ClinicalDocument();

            MakeCCDAHeader(ccda);

            MakeCCDABody(ccda);

            Console.Clear();
            Console.WriteLine("CCD Generated");
            Console.ReadKey();

            //ValidateCCDA(ccda);

            XmlStateWriter xsw = new XmlStateWriter(XmlWriter.Create("D:\\EverestPoC.xml", new XmlWriterSettings() { Indent = true }));
            DateTime start = DateTime.Now;
            var result = fmtr.Graph(xsw, ccda);
            xsw.Flush();
        }

        private static void MakeCCDAHeader(ClinicalDocument ccda)
        {
            MakeStaticSection(ccda);

            MakeRecordTargetNode(ccda);

            MakeAuthorNode(ccda);

            MakeDataEntererNode(ccda);

            MakeCustodianNode(ccda);

            MakeInformationRecipientNode(ccda);

            MakeLegalAuthenticatorNode(ccda);

            MakeParticipantNode(ccda);

            MakeDocumentationOfNode(ccda);

            MakeComponentOfNode(ccda);
        }

        private static void MakeComponentOfNode(ClinicalDocument ccda)
        {
            AssignedEntity ae = MakeAssignedEntity("ComponentOf");

            EncompassingEncounter ee = new EncompassingEncounter();
            ee.Id = new SET<II>(new II(new Guid()));
            ee.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), new TS(DateTime.Now));

            ee.ResponsibleParty = new ResponsibleParty();

            ee.ResponsibleParty.AssignedEntity = ae;

            Location loc = new Location();
            loc.HealthCareFacility = MakeHealthCareFacility();

            ee.Location = new Location();
            ee.Location = loc;

            Component1 componentOf = new Component1();
            componentOf.EncompassingEncounter = new EncompassingEncounter();
            componentOf.EncompassingEncounter = ee;

            ccda.ComponentOf = new Component1();
            ccda.ComponentOf = componentOf;
        }

        private static HealthCareFacility MakeHealthCareFacility()
        {
            HealthCareFacility hcf = new HealthCareFacility();
            hcf.Id = new SET<II>(new II("1.1.1.1.1.1.1.1.2"));

            //TODO: Code System Name should be an enum with possible values like NUCC and LOINC etc
            hcf.Code = new CE<string>("261QP2300X",
                "2.16.840.1.113883.6.101",
                "NUCC",
                null,
                "Primary Care",
                null);

            hcf.Location = new Place();
            //hcf.Location.Name = 
            //new ENXP("Primo Adult Health"));

            hcf.Location.Addr = new AD(
                new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)});

            hcf.ServiceProviderOrganization = new Organization();
            hcf.ServiceProviderOrganization.Id = new SET<II>(new II("1.1.1.1.1.1.1.1.2"));
            //hcf.ServiceProviderOrganization.Name = new EN().Part.Add(new ENXP("Primo Adult Health"));
            hcf.ServiceProviderOrganization.Telecom = new SET<TEL>(
                new TEL("tel:+1(571)555-0179;", TelecommunicationAddressUse.WorkPlace));
            hcf.ServiceProviderOrganization.Addr = new SET<AD>(new AD(
                new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));
            hcf.ServiceProviderOrganization.StandardIndustryClassCode = new CE<string>("261QP2300X",
                "2.16.840.1.113883.6.101",
                "NUCC",
                null,
                "Primary Care",
                null);

            return hcf;
        }

        private static AssignedEntity MakeAssignedEntity(string section)
        {
            AssignedEntity ae = new AssignedEntity();

            if (section == "Legal Authentication" || section == "ComponentOf")
            {
                //NPI 12345
                ae.Id = new SET<II>(new II(
                    "2.16.840.1.113883.4.6",
                    "12345"));

                ae.Code = new CE<string>("207QA0505X",
                    "2.16.840.1.113883.6.101",
                    "NUCC",
                    null,
                    "Adult Medicine",
                    null);

                ae.Addr = new SET<AD>(
                    new AD(
                        new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

                ae.Telecom = new SET<TEL>(
                    new TEL("tel:+1(571)555-0179;ext=221",
                        TelecommunicationAddressUse.WorkPlace));

                ae.AssignedPerson = new Person(new SET<PN>(
                    new PN(
                        new List<ENXP>{
                    new ENXP("Raymond",EntityNamePartType.Given),
                    new ENXP("Boccino",EntityNamePartType.Family),
                    new ENXP("MD",EntityNamePartType.Suffix)})));
            }

            if (section == "DocumentationOf")
            {
                //NPI 34567
                ae.Id = new SET<II>(new II(
                    "2.16.840.1.113883.4.6",
                    "34567"));

                ae.Code = new CE<string>("207RC0000X",
                    "2.16.840.1.113883.6.101",
                    "NUCC",
                    null,
                    "Cardiovascular Disease",
                    null);

                ae.Addr = new SET<AD>(
                    new AD(
                        new ADXP[]{
                            new ADXP("209 County Line Rd", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

                ae.Telecom = new SET<TEL>(
                    new TEL("tel:+1(571)555-0155",
                        TelecommunicationAddressUse.WorkPlace));

                ae.AssignedPerson = new Person(new SET<PN>(
                    new PN(
                        new List<ENXP>{
                    new ENXP("Dwayne",EntityNamePartType.Given),
                    new ENXP("Forge",EntityNamePartType.Family),
                    new ENXP("MD",EntityNamePartType.Suffix)})));
            }

            if (section == "Data Enterer")
            {
                ae.Id = new SET<II>(new II(
                "1.1.1.1.1.1.1.1.2",
                "678910"));

                ae.Code = new CE<string>("364SA2200X",
                    "2.16.840.1.113883.6.101",
                    "NUCC",
                    null,
                    "Adult Health",
                    null);

                ae.Addr = new SET<AD>(
                    new AD(
                        new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

                ae.Telecom = new SET<TEL>(
                    new TEL("tel:+1(571)555-0179;ext=222",
                        TelecommunicationAddressUse.WorkPlace));

                ae.AssignedPerson = new Person(new SET<PN>(
                    new PN(
                        new List<ENXP>{
                    new ENXP("Mallory",EntityNamePartType.Given),
                    new ENXP("Bardas",EntityNamePartType.Family),
                    new ENXP("RN",EntityNamePartType.Suffix)})));
            }

            if (section == "PROCEDURES")
            {
                //NPI 34567
                ae.Id = new SET<II>(new II(
                    "2.16.840.1.113883.4.6",
                    "34567"));

                ae.Code = new CE<string>("207RC0000X",
                    "2.16.840.1.113883.6.101",
                    "NUCC",
                    null,
                    "Cardiovascular Disease",
                    null);

                ae.Addr = new SET<AD>(
                    new AD(
                        new ADXP[]{
                            new ADXP("209 County Line Rd", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

                ae.Telecom = new SET<TEL>(
                    new TEL("tel:+1(571)555-0155",
                        TelecommunicationAddressUse.WorkPlace));

                ae.AssignedPerson = new Person(new SET<PN>(
                    new PN(
                        new List<ENXP>{
                    new ENXP("Dwayne",EntityNamePartType.Given),
                    new ENXP("Forge",EntityNamePartType.Family),
                    new ENXP("MD",EntityNamePartType.Suffix)})));

                ON on = new ON();
                on.Part.Add(new ENXP("Primo Adult Health"));
                ae.RepresentedOrganization = new Organization(
                    new SET<II>(new II("1.1.1.1.1.1.1.1.2")),
                    new SET<ON>(on),
                    new SET<TEL>(new TEL("tel:+1(571)555-0155")),
                new SET<AD>(
                    new AD(
                        new ADXP[]{
                            new ADXP("209 County Line Rd", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)})),
                            null,
                            null);

            }

            return ae;
        }

        private static void MakeDocumentationOfNode(ClinicalDocument ccda)
        {
            ServiceEvent se = new ServiceEvent();
            se.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), new TS(DateTime.Now));

            Performer1 performer = new Performer1();
            performer.AssignedEntity = MakeAssignedEntity("DocumentationOf");
            performer.TypeCode = new CS<x_ServiceEventPerformer>(x_ServiceEventPerformer.PRF);

            DocumentationOf docOf = new DocumentationOf();
            docOf.ServiceEvent = new ServiceEvent();
            docOf.ServiceEvent = se;

            ccda.DocumentationOf = new List<DocumentationOf>();
            ccda.DocumentationOf.Add(docOf);
        }

        private static void MakeParticipantNode(ClinicalDocument ccda)
        {
            Participant1 participant = new Participant1();
            participant.TypeCode = new CS<ParticipationType>(ParticipationType.IND);

            participant.AssociatedEntity = new AssociatedEntity();

            participant.AssociatedEntity.ClassCode = new CS<RoleClassAssociative>(RoleClassAssociative.PersonalRelationship);

            participant.AssociatedEntity.Addr = new SET<AD>(
                new AD(
                    new ADXP[]{
                            new ADXP("100 Marshall Lane", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22153", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));
            participant.AssociatedEntity.Telecom = new SET<TEL>(new TEL("tel:+1(571)555-0179;ext=221"));

            participant.AssociatedEntity.AssociatedPerson = new Person();
            participant.AssociatedEntity.AssociatedPerson.Name = new SET<PN>(
                new PN(
                    new List<ENXP>{
                    new ENXP("Kathleen", EntityNamePartType.Given),
                    new ENXP("McReary", EntityNamePartType.Family)}));

            ccda.Participant = new List<Participant1>();
            ccda.Participant.Add(participant);
        }

        private static void MakeLegalAuthenticatorNode(ClinicalDocument ccda)
        {
            LegalAuthenticator la = new LegalAuthenticator();
            la.Time = DateTime.Now;
            la.SignatureCode = new CS<string>("S");
            la.AssignedEntity = MakeAssignedEntity("Legal Authentication");

            ccda.LegalAuthenticator = new LegalAuthenticator();
            ccda.LegalAuthenticator = la;
        }

        private static void MakeInformationRecipientNode(ClinicalDocument ccda)
        {
            IntendedRecipient ir = new IntendedRecipient();

            //NPI 23456
            ir.Id = new SET<II>(new II(
                "2.16.840.1.113883.4.6",
                "23456"));

            ir.InformationRecipient = new Person(new SET<PN>(
                new PN(
                    new List<ENXP>{
                    new ENXP("Bernard",EntityNamePartType.Given),
                    new ENXP("Crane",EntityNamePartType.Family),
                    new ENXP("MD",EntityNamePartType.Suffix)})));

            ON on = new ON();
            on.Part.Add(new ENXP("Springfield Geriatric Associates"));

            Organization org = new Organization(
                null,
                new SET<ON>(on),
                new SET<TEL>(new TEL("tel:+1(571)555-0165", TelecommunicationAddressUse.WorkPlace)),
                new SET<AD>(new AD(
                    new ADXP[]{
                            new ADXP("202 County Line Rd", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)})),
                new CE<string>("207QG0300X",
                    "2.16.840.1.113883.6.101",
                    "NUCC",
                    null,
                    "Geriatric Medicine",
                    null),
                null);

            ir.ReceivedOrganization = org;

            InformationRecipient infor = new InformationRecipient();
            infor.IntendedRecipient = ir;

            ccda.InformationRecipient = new List<InformationRecipient>();
            ccda.InformationRecipient.Add(infor);

        }

        private static void MakeCustodianNode(ClinicalDocument ccda)
        {
            CustodianOrganization rco = new CustodianOrganization();

            rco.Id = new SET<II>(new II("1.1.1.1.1.1.1.1.2"));
            ON on = new ON();
            on.Part.Add(new ENXP("Primo Adult Health"));
            rco.Name = on;

            rco.Telecom = new TEL("tel:+1(571)555-0179;ext=222",
                    TelecommunicationAddressUse.WorkPlace);

            rco.Addr = new AD(
                    new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)});


            AssignedCustodian ac = new AssignedCustodian();
            ac.RepresentedCustodianOrganization = rco;

            Custodian custodian = new Custodian();
            custodian.AssignedCustodian = ac;
            ccda.Custodian = custodian;
        }

        private static void MakeDataEntererNode(ClinicalDocument ccda)
        {
            ccda.DataEnterer = new DataEnterer();
            ccda.DataEnterer.AssignedEntity = MakeAssignedEntity("Data Enterer");

        }

        private static void MakeAuthorNode(ClinicalDocument ccda)
        {
            AssignedAuthor aa = new AssignedAuthor();

            //NPI 12345
            aa.Id = new SET<II>(new II("2.16.840.1.113883.4.6", "12345"));

            aa.Code = new CE<string>("207QA0505X",
                "2.16.840.1.113883.6.101",
                "NUCC",
                null,
                "Adult Medicine",
                null);

            aa.Addr = new SET<AD>(
                new AD(
                    new ADXP[]{
                            new ADXP("1400 Main Street Ste G", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22150", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

            aa.Telecom = new SET<TEL>(
                new TEL("tel:+1(571)555-0179;ext=221",
                    TelecommunicationAddressUse.WorkPlace));

            aa.SetAssignedAuthorChoice(new SET<PN>(
                new PN(
                    new List<ENXP>{
                    new ENXP("Raymond",EntityNamePartType.Given),
                    new ENXP("Boccino",EntityNamePartType.Family),
                    new ENXP("MD",EntityNamePartType.Suffix)})));

            Author a = new Author();
            a.Time = DateTime.Now;
            a.AssignedAuthor = aa;

            ccda.Author.Add(a);

        }

        private static void MakeRecordTargetNode(ClinicalDocument ccda)
        {
            //CONF 5266 - Record Target
            RecordTarget rt = new RecordTarget();
            rt.ContextControlCode = ContextControl.OverridingPropagating;

            //CONF 5267
            MakePatientRoleNode(rt);

            ccda.RecordTarget.Add(rt);
        }

        private static void MakePatientRoleNode(RecordTarget rt)
        {
            PatientRole pr = new PatientRole();

            //CONF 5268
            pr.Id = new SET<II>(new II(
                "2.16.840.1.113883.4.1",
                //Patient SSN recorded as an ID
                "123-456-7890"));

            //CONF 5271
            pr.Addr = new SET<AD>(
                new AD(new CS<PostalAddressUse>(PostalAddressUse.PrimaryHome),
                    new ADXP[]{
                            new ADXP("100 Marshall Lane", AddressPartType.StreetAddressLine),
                            new ADXP("Springfield", AddressPartType.City),
                            new ADXP("VA", AddressPartType.State),
                            new ADXP("22153", AddressPartType.PostalCode),
                            new ADXP("US", AddressPartType.Country)}));

            //CONF 5280
            pr.Telecom = new SET<TEL>(
                new TEL(
                    "tel:+1(571)555-0189",
                    TelecommunicationAddressUse.PrimaryHome));

            //CONF 5283 
            MakePatientNode(pr);

            rt.PatientRole = pr;
        }

        private static void MakePatientNode(PatientRole pr)
        {
            //Patient p = new Patient();
            MyPatientMultipleRaceCodes p = new MyPatientMultipleRaceCodes();

            //CONF 5284
            //L is "Legal" from HL7 EntityNameUse 2.16.840.1.113883.5.45
            //p.Name = new SET<PN>();
            //p.Name.Add(new PN(EntityNameUse.Legal,
            //    new ENXP("Nikolai", EntityNamePartType.Given)));
            //p.Name.Add(new PN(EntityNameUse.Legal,
            //    new ENXP("Bellic", EntityNamePartType.Family)));

            p.Name = new SET<PN>(
                new PN(
                    new List<ENXP>{
                    new ENXP("Nikolai", EntityNamePartType.Given),
                    new ENXP("Bellic", EntityNamePartType.Family)}));

            p.AdministrativeGenderCode = new CE<string>("M",
                "2.16.840.1.113883.5.1",
                "AdministrativeGender",
                null,
                "Male",
                null);

            p.BirthTime = DateTime.Now;

            p.MaritalStatusCode = new CE<string>("M",
                "2.16.840.1.113883.5.2",
                "MaritalStatus",
                null,
                "Married",
                null);

            p.ReligiousAffiliationCode = new CE<string>("1041",
                "2.16.840.1.113883.5.1076",
                "ReligiousAffiliation",
                null,
                "Roman Catholic",
                null);

            //p.RaceCode = new CE<string>("2106-3",
            //    "2.16.840.1.113883.6.238",
            //    "OMB Standards for Race and Ethnicity",
            //    null,
            //    "White",
            //    null);

            p.RaceCode = new SET<CE<string>>();
            p.RaceCode.Add(
                new CE<string>("2106-3",
                "2.16.840.1.113883.6.238",
                "OMB Standards for Race and Ethnicity",
                null,
                "White",
                null));
            p.RaceCode.Add(
                new CE<string>("2131-1",
                "2.16.840.1.113883.6.238",
                "Test",
                null,
                "Other Race",
                null));

            p.EthnicGroupCode = new CE<string>("2186-5",
                "2.16.840.1.113883.6.238",
                "OMB Standards for Race and Ethnicity",
                null,
                "Not Hispanic or Latino",
                null);

            //CONF 5407: LanguageCode Code System 2.16.840.1.113883.1.11.11526
            p.LanguageCommunication.Add(
                new LanguageCommunication(
                    new CS<String>("eng"),
                    new CE<string>("ESP",
                        "2.16.840.1.113883.5.60",
                        "LanguageAbilityMode",
                        null,
                        "Expressed spoken",
                        null),
                    null,
                    null));

            pr.Patient = p;
        }

        private static void MakeStaticSection(ClinicalDocument ccda)
        {
            //CONF 16791
            ccda.RealmCode = new SET<CS<BindingRealm>>(new CS<BindingRealm>(BindingRealm.UnitedStatesOfAmerica));

            //CONF 5361 
            ccda.TypeId = new II(
                StaticCCDAData["ClinicalDocumentTypeIdRoot"],
                StaticCCDAData["ClinicalDocumentTypeIdExtension"]);

            //CONF 5252
            ccda.TemplateId = new LIST<II>();
            ccda.TemplateId.Add(new II(
                "2.16.840.1.113883.10.20.22.1.1"));
            //The next templateId, code and title will differ depending on what type of document is being sent.
            //Confirms to the document specific requirements
            ccda.TemplateId.Add(new II(
                "2.16.840.1.113883.10.20.22.1.2"));

            //CONF 5363 
            ccda.Id = new II(
                "1.1.1.1.1.1.1.1.1", "Test CCDA");

            //CONF 5253 "CCD document"
            ccda.Code = new CE<string>(
                "34133-9",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Summarization of Episode Note",
                null);

            //CONF 5254
            ccda.Title = new ST("Primo Adult Health: Health Summary");

            //CONF 5256
            ccda.EffectiveTime = DateTime.Now;

            //CONF 5259
            ccda.ConfidentialityCode = new CE<x_BasicConfidentialityKind>(
                x_BasicConfidentialityKind.Normal,
                "2.16.840.1.113883.5.25");

            //CONF 5372
            ccda.LanguageCode = new CS<string>("en-US");

        }

        private static void ValidateCCDA(ClinicalDocument ccda)
        {
            throw new NotImplementedException();
        }

        private static void MakeCCDABody(ClinicalDocument ccda)
        {
            StructuredBody sb = new StructuredBody();

            AddAdvancedDirectivesComponent(sb);
            AddDischargeInstructionsComponent(sb);
            AddAllergiesComponent(sb);
            AddReasonForVisitComponent(sb);
            AddFamilyHistoryComponent(sb);
            AddFucntionalAndConginitiveStatusComponent(sb);
            AddImmunizationComponent(sb);
            AddInstructionComponent(sb);
            AddMedicationComponent(sb);
            AddPlanOfCareComonent(sb);
            AddProblemListComponent(sb);
            AddProcedureComponent(sb);
            AddReasonForReferralComponent(sb);
            AddResultsSection(sb);
            AddSocialHistoryComponent(sb);
            AddVitalSignsComponent(sb);

            Component2 comp2 = new Component2(ActRelationshipHasComponent.HasComponent, true);
            comp2.SetBodyChoice(sb);

            ccda.Component = comp2;
        }

        private static void AddResultsSection(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.3.1"));

            section.Code = new CE<string>(
                "30954-2",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "RESULTS",
                null);

            section.Title = new ST("RESULTS");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"result1\">TSH - 6 mIU/L (abnormal, high)</content></item><item><content ID=\"result2\">Hgb a1c - 8% (abnormal, high)</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeOrganizer());
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
		
        }

        private static Organizer MakeOrganizer()
        {
            Organizer o = new Organizer();
            o.ClassCode = new CS<x_ActClassDocumentEntryOrganizer>(x_ActClassDocumentEntryOrganizer.BATTERY);
            o.TemplateId = new LIST<II>();
            o.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.1"));
            o.Id = new SET<II>(new II(new Guid()));
            o.Code = new CD<string>(
                "11579-0",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Thyrotropin [Units/volume] in Serum or Plasma by Detection limit less than or equal to 0.05 mIU/L",
                null);
            o.StatusCode = new CS<ActStatus>(ActStatus.Completed);

            Observation obs = new Observation();
            //It automatically adds class code
            obs.MoodCode = new CS<x_ActMoodDocumentObservation>(x_ActMoodDocumentObservation.Eventoccurrence);
            obs.TemplateId = new LIST<II>();
            obs.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.2"));
            obs.Id = new SET<II>(new II(new Guid()));
            obs.Code = new CD<string>(
                "3016-3",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Thyrotropin [Units/volume] in Serum or Plasma",
                null);
            obs.Text = new ED();
            obs.Text.Reference = new TEL("#result1");
            obs.StatusCode = new CS<ActStatus>(ActStatus.Completed);
            obs.EffectiveTime = new IVL<TS>();
            obs.EffectiveTime.Value = new TS(DateTime.Today);
            obs.Value = new PQ(6m, "mIU/L");
            obs.InterpretationCode = new SET<CE<string>>();
            obs.InterpretationCode.Add(
                 new CE<string>(
                     "A",
                     "2.16.840.1.113883.5.83",
                     "ObservationInterpretation",
                     null,
                     "abnormal",
                     null));
            obs.InterpretationCode.Add(
                 new CE<string>(
                     "H",
                     "2.16.840.1.113883.5.83",
                     "ObservationInterpretation",
                     null,
                     "high",
                     null));
            obs.ReferenceRange = new List<ReferenceRange>();
            obs.ReferenceRange.Add(new ReferenceRange(
                new ObservationRange(
                    null,
                    new ED("normal: 0.29–5.11 mIU/L"),
                    null,
                    null)));

            Component4 comp = new Component4();
            comp.SetClinicalStatement(obs);

            o.Component = new List<Component4>();
            o.Component.Add(comp);            

            return o;
        }

        private static void AddProcedureComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.7.1"));

            section.Code = new CE<string>(
                "47519-4",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "HISTORY OF PROCEDURES",
                null);

            section.Title = new ST("PROCEDURES");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"procedure1\">EKG (2012/10/15)</content></item><item><content ID=\"procedure2\">Cholecystectomy (2006/06/01)</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeProcedure());
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);            
        }

        private static Procedure MakeProcedure()
        {
            Procedure p = new Procedure();
            //It automatically adds class code
            p.MoodCode = new CS<x_DocumentProcedureMood>(x_DocumentProcedureMood.Eventoccurrence);
            p.TemplateId = new LIST<II>();
            p.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.14"));
            p.Id = new SET<II>(new II(new Guid()));
            p.Code = new CD<string>(
                "49038010",
                "2.16.840.1.113883.6.96",
                "SNOMED CT",
                null,
                "EKG",
                null);
            p.Code.OriginalText = new ED();
            p.Code.OriginalText.Reference = new TEL("#procedure1");
            p.StatusCode = new CS<ActStatus>(ActStatus.Completed);
            p.EffectiveTime = new IVL<TS>(new TS(DateTime.Today), new TS(DateTime.Today));

            Performer2 perf = new Performer2();
            perf.AssignedEntity = MakeAssignedEntity("PROCEDURES");

            p.Performer = new List<Performer2>();
            p.Performer.Add(perf);
                                    
            return p;
        }

        private static void AddProblemListComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.5.1"));

            section.Code = new CE<string>(
                "11450-4",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "PROBLEM LIST",
                null);

            section.Title = new ST("PROBLEM LIST");

            section.Text = new ED();
            section.Text = "<content ID=\"problems\"/><list listType=\"ordered\"><item><content ID=\"problem1\">Hypertension</content></item><item><content ID=\"problem2\">Hyperlipidemia</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeAct("PROBLEM LIST"));
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
		
        }

        private static void AddSocialHistoryComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.17"));

            section.Code = new CE<string>(
                "29762-2",
                "2.16.840.1.113883.6.1",
                null,
                null,
                "SOCIAL HISTORY",
                null);

            section.Title = new ST("SOCIAL HISTORY");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"sochist1\">50 pack year smoking history, quit 1997</content></item><item><content ID=\"sochist2\">etoh (alcohol) daily, patient reports varies from 1 to many cocktails per day</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeObservation("SOCIAL HISTORY"));
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
		
        }

        private static void AddReasonForReferralComponent(StructuredBody sb)
        {

            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("1.3.6.1.4.1.19376.1.5.3.1.3.1"));

            section.Code = new CE<string>(
                "42349-1",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "REASON FOR REFERRAL",
                null);

            section.Title = new ST("REASON FOR REFERRAL");

            section.Text = new ED();
            section.Text = "<paragraph>Geriatric assessment and management for concerns about dementia and falling</paragraph>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
		
        }

        private static void AddPlanOfCareComonent(StructuredBody sb)
        {

            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.10"));

            section.Code = new CE<string>(
                "18776-5",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Treatment plan",
                null);

            section.Title = new ST("PLAN OF CARE");

            section.Text = new ED();
            section.Text = "<paragraph>I have discussed this request for geriatric assessment and management with the patient and his wife who agree with this plan.</paragraph>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
		
        }

        private static void AddMedicationComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.1.1"));

            section.Code = new CE<string>(
                "10160-0",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "HISTORY OF MEDICATION USE",
                null);

            section.Title = new ST("MEDICATIONS");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"medication1\">Lisinopril - 20mg by mouth once daily</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeSubstanceAdministration("MEDICATIONS"));
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);

        }

        private static void AddInstructionComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.45"));
            section.Id = new II(new Guid());

            section.Code = new CE<string>(
                "69730-0",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Instructions",
                null);

            section.Title = new ST("INSTRUCTIONS");

            section.Text = new ED();
            section.Text = "<paragraph>Patient is to schedule follow-up appointment with PCP after consultation.</paragraph>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);

        }

        private static void AddFucntionalAndConginitiveStatusComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.14"));

            section.Code = new CE<string>(
                "47420-5",
                "2.16.840.1.113883.6.1");

            section.Title = new ST("FUNCTIONAL STATUS");

            section.Text = new ED();
            section.Text = "Cognitive impairment. Patient uses cane for walking.";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);

        }

        private static void AddFamilyHistoryComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.15"));

            section.Code = new CE<string>(
                "10157-6",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Family History",
                null);

            section.Title = new ST("FAMILY HISTORY");

            section.Text = new ED();
            section.Text = "<paragraph>Non-contributory</paragraph>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);


        }

        private static void AddReasonForVisitComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.12"));

            section.Code = new CE<string>(
                "29299-5",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "REASON FOR VISIT",
                null);

            section.Title = new ST("REASON FOR VISIT");

            section.Text = new ED();
            section.Text = "<paragraph>Recent falls</paragraph>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);

        }

        private static void AddAllergiesComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.6.1"));

            section.Code = new CE<string>("48765-2", "2.16.840.1.113883.6.1");

            section.Title = new ST("ALLERGIES");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"allergy1\">Allergy to Penicillin drugs</content></item><item><content ID=\"allergy2\">Allergy to Bee Pollen</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.TypeCode = new CS<x_ActRelationshipEntry>(x_ActRelationshipEntry.DRIV);
            e.SetClinicalStatement(MakeAct("ALLERGIES"));
            section.Entry = new List<Entry>();
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);

        }

        private static SubstanceAdministration MakeSubstanceAdministration(string section)
        {
            SubstanceAdministration sa = new SubstanceAdministration();

            if (section == "IMMUNIZATIONS")
            {
                sa.TemplateId = new LIST<II>();
                sa.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.52"));
                sa.Id = new SET<II>(new II(new Guid()));
                sa.StatusCode = new CS<ActStatus>(ActStatus.Completed);
                sa.EffectiveTime.Add(new GTS());
                //It automatically adds class code
                sa.MoodCode = new CS<x_DocumentSubstanceMood>(x_DocumentSubstanceMood.Eventoccurrence);
                sa.NegationInd = true;

                sa.Consumable = MakeConsumable("IMMUNIZATIONS");
            }

            if (section == "MEDICATIONS")
            {
                sa.MoodCode = new CS<x_DocumentSubstanceMood>(x_DocumentSubstanceMood.Eventoccurrence);
                sa.TemplateId = new LIST<II>();
                sa.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.16"));
                sa.Id = new SET<II>(new II(new Guid()));
                sa.Text = new ED();
                sa.Text.Reference = new TEL("#medication1");
                sa.StatusCode = new CS<ActStatus>(ActStatus.Active);
                //TODO:
                sa.EffectiveTime = new List<GTS>();
                sa.EffectiveTime.Add(new GTS(new IVL<TS>(new TS(DateTime.Today), new TS(DateTime.Today))));
                PIVL<TS> p = new PIVL<TS>();
                p.InstitutionSpecified = true;
                p.Operator = SetOperator.A;
                p.Period = new PQ(24.0m, "h");
                sa.EffectiveTime.Add(new GTS(p));
                sa.RouteCode = new CE<string>(
                    "C38288",
                    "2.16.840.1.113883.3.26.1.1",
                    "NCI Thesaurus",
                    null,
                    "oral",
                    null);
                sa.DoseQuantity = new IVL<PQ>(new PQ());
                sa.AdministrationUnitCode = new CE<string>(
                    "C42998",
                    "2.16.840.1.113883.3.26.1.1",
                    "NCI Thesaurus",
                    null,
                    "tablet",
                    null);
                sa.Consumable = MakeConsumable("MEDICATIONS");
            }

            return sa;
        }

        private static Consumable MakeConsumable(string section)
        {
            Consumable c = new Consumable();

            if (section == "IMMUNIZATIONS")
            {
                ManufacturedProduct mp = new ManufacturedProduct();
                mp.ClassCode = new CS<RoleClassManufacturedProduct>(RoleClassManufacturedProduct.ManufacturedProduct);
                mp.TemplateId = new LIST<II>();
                mp.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.54"));

                mp.ManufacturedDrugOrOtherMaterial = new Material();

                Material m = new Material();
                m.Code = new CE<string>(
                    "140",
                    "2.16.840.1.113883.12.292",
                    "Vaccines administered (CVX)",
                    null,
                    "Influenza, seasonal, injectable, preservative free",
                    null);
                m.Code.OriginalText = new ED();
                m.Code.OriginalText.MediaType = "text/x-hl7-text+xml";
                m.Code.OriginalText.Representation = EncapsulatedDataRepresentation.XML;
                m.Code.OriginalText.Reference = new TEL();
                m.Code.OriginalText.Reference.Value = "#immunization1";
                mp.ManufacturedDrugOrOtherMaterial = m;

                ON on = new ON();
                on.Part.Add(new ENXP("Influenza Vaccine Company"));
                mp.ManufacturerOrganization = new Organization();
                mp.ManufacturerOrganization.Name = new SET<ON>();
                mp.ManufacturerOrganization.Name.Add(on);
                
                c.ManufacturedProduct = new ManufacturedProduct();
                c.ManufacturedProduct = mp;
            }

            if (section == "MEDICATIONS")
            {
                ManufacturedProduct mp = new ManufacturedProduct();
                mp.ClassCode = new CS<RoleClassManufacturedProduct>(RoleClassManufacturedProduct.ManufacturedProduct);
                mp.TemplateId = new LIST<II>();
                mp.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.23"));

                mp.ManufacturedDrugOrOtherMaterial = new Material();

                Material m = new Material();
                m.Code = new CE<string>(
                    "314077",
                    "2.16.840.1.113883.6.88",
                    "RxNorm",
                    null,
                    "Lisinopril 20 MG Oral Tablet",
                    null);
                m.Code.OriginalText = new ED();
                m.Code.OriginalText.MediaType = "text/x-hl7-text+xml";
                m.Code.OriginalText.Representation = EncapsulatedDataRepresentation.XML;
                m.Code.OriginalText.Reference = new TEL();
                m.Code.OriginalText.Reference.Value = "#medication1";
                mp.ManufacturedDrugOrOtherMaterial = m;

                c.ManufacturedProduct = new ManufacturedProduct();
                c.ManufacturedProduct = mp;
            }

            return c;
        }

        private static Act MakeAct(string section)
        {
            Act a = new Act();
            if (section == "ALLERGIES")
            {
                a.ClassCode = new CS<x_ActClassDocumentEntryAct>(x_ActClassDocumentEntryAct.Act);
                a.MoodCode = new CS<x_DocumentActMood>(x_DocumentActMood.Eventoccurrence);
                a.TemplateId = new LIST<II>();
                a.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.30"));
                a.Id = new SET<II>(new II(new Guid()));
                a.Code = new CD<string>(
                    "48765-2",
                    "2.16.840.1.113883.6.1",
                    "LOINC",
                    null,
                    "Allergies, adverse reactions, alerts",
                    null);
                a.StatusCode = new CS<ActStatus>(ActStatus.Active);
                a.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), null);

                a.EntryRelationship.Add(MakeEntryRelationship("ALLERGIES"));
            }

            if (section == "PROBLEM LIST")
            {
                a.ClassCode = new CS<x_ActClassDocumentEntryAct>(x_ActClassDocumentEntryAct.Act);
                a.MoodCode = new CS<x_DocumentActMood>(x_DocumentActMood.Eventoccurrence);
                a.TemplateId = new LIST<II>();
                a.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.3"));
                a.Id = new SET<II>(new II(new Guid()));
                a.Code = new CD<string>(
                    "CONC",
                    "2.16.840.1.113883.5.6",
                    null,
                    null,
                    "Concern",
                    null);
                a.StatusCode = new CS<ActStatus>(ActStatus.Active);
                a.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), null);

                a.EntryRelationship.Add(MakeEntryRelationship("PROBLEM LIST"));
            }

            return a;
        }

        private static EntryRelationship MakeEntryRelationship(string section)
        {
            EntryRelationship er = new EntryRelationship();
            er.TypeCode = new CS<x_ActRelationshipEntryRelationship>(x_ActRelationshipEntryRelationship.SUBJ);

            if (section == "ALLERGIES")
            {
                er.InversionInd = true;
                er.SetClinicalStatement(MakeObservation("ALLERGIES"));
            }

            if (section == "PROBLEM LIST")
            {
                er.SetClinicalStatement(MakeObservation("PROBLEM LIST"));
            }

            return er;
        }

        private static Observation MakeObservation(string section)
        {
            Observation o = new Observation();

            if (section == "ALLERGIES")
            {
                //It automatically adds class code
                o.MoodCode = new CS<x_ActMoodDocumentObservation>(x_ActMoodDocumentObservation.Eventoccurrence);
                o.TemplateId = new LIST<II>();
                o.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.7"));
                o.Id = new SET<II>(new II(new Guid()));
                o.Code = new CD<string>(
                    "ASSERTION",
                    "2.16.840.1.113883.5.4",
                    null,
                    null,
                    null,
                    null);
                o.StatusCode = new CS<ActStatus>(ActStatus.Completed);
                o.EffectiveTime = new IVL<TS>(new TS());
                o.EffectiveTime.Low = new TS(DateTime.Now);
                o.EffectiveTime.Low.NullFlavor = new CS<NullFlavor>(NullFlavor.Unknown);

                o.Value = new CD<string>(
                    "416098002",
                    "2.16.840.1.113883.6.96",
                    "SNOMED CT",
                    null,
                    "drug allergy",
                    new ED(new TEL("#allergy1")));

                Participant2 participant2 = new Participant2();
                participant2.TypeCode = new CS<ParticipationType>(ParticipationType.Consumable);
                participant2.ParticipantRole = new ParticipantRole();
                participant2.ParticipantRole.ClassCode = new CS<string>("MANU");
                PlayingEntity pe = new PlayingEntity(null,
                    new CE<string>("70618", "2.16.840.1.113883.6.88", "RxNorm", null, "Penicillin", null),
                    null,
                    null,
                    null);
                pe.Code.OriginalText = new ED();
                pe.Code.OriginalText.Reference = new TEL("#allergy1");
                pe.ClassCode = new CS<EntityClassRoot>(new EntityClassRoot());

                participant2.ParticipantRole.SetPlayingEntityChoice(pe);
                o.Participant.Add(participant2);

                o.EntryRelationship = new List<EntryRelationship>();
                EntryRelationship er = new EntryRelationship();
                er.TypeCode = new CS<x_ActRelationshipEntryRelationship>(x_ActRelationshipEntryRelationship.MFST);
                er.InversionInd = new BL(true);

                Observation obs = new Observation();
                obs.MoodCode = new CS<x_ActMoodDocumentObservation>(x_ActMoodDocumentObservation.Eventoccurrence);
                obs.TemplateId = new LIST<II>();
                obs.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.8"));
                obs.Code = new CD<string>("SEV", "2.16.840.1.113883.5.4", "ActCode", null, null, null);
                obs.Text = new ED();
                obs.Text.Reference = new TEL("#allergy1");
                obs.StatusCode = new CS<ActStatus>();
                obs.StatusCode.Code = new MARC.Everest.DataTypes.Primitives.CodeValue<ActStatus>(ActStatus.Completed);
                obs.Value = new CD<string>(
                    "6736007",
                    "2.16.840.1.113883.6.96",
                    "SNOMED CT",
                    null,
                    "moderate",
                    null);

                er.SetClinicalStatement(obs);
                o.EntryRelationship.Add(er);
            }

            if (section == "SOCIAL HISTORY")
            {
                //It automatically adds class code
                o.MoodCode = new CS<x_ActMoodDocumentObservation>(x_ActMoodDocumentObservation.Eventoccurrence);
                o.TemplateId = new LIST<II>();
                o.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.78"));
                o.Id = new SET<II>(new II(new Guid()));
                o.Code = new CD<string>(
                    "ASSERTION",
                    "2.16.840.1.113883.5.4",
                    null,
                    null,
                    null,
                    null);
                o.StatusCode = new CS<ActStatus>(ActStatus.Completed);
                o.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), new TS(DateTime.Now));

                o.Value = new CD<string>(
                    "8517006",
                    "2.16.840.1.113883.6.96",
                    "SNOMED CT",
                    null,
                    "former smoker",
                    null);
            }

            if (section == "PROBLEM LIST")
            {
                //It automatically adds class code
                o.MoodCode = new CS<x_ActMoodDocumentObservation>(x_ActMoodDocumentObservation.Eventoccurrence);
                o.TemplateId = new LIST<II>();
                o.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.4.4"));
                o.Id = new SET<II>(new II(new Guid()));
                o.Code = new CD<string>(
                    "64572001",
                    "2.16.840.1.113883.6.96",
                    "SNOMED CT",
                    null,
                    "Condition",
                    null);
                o.Text = new ED();
                o.Text.Reference = new TEL("#problem1");
                o.StatusCode = new CS<ActStatus>(ActStatus.Completed);
                o.EffectiveTime = new IVL<TS>(new TS(DateTime.Now), null);

                o.Value = new CD<string>(
                    "5962100",
                    "2.16.840.1.113883.6.96",
                    "SNOMED CT",
                    null,
                    "Essential Hypertension",
                    null);
            }

            return o;
        }

        private static void AddDischargeInstructionsComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.41"));

            section.Code = new CE<string>(
                "8653-8",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "HOSPITAL DISCHARGE INSTRUCTIONS",
                null);

            section.Title = new ST("Hospital Discharge Instructions");

            section.Text = new ED();
            section.Text = "";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
        }

        private static void AddImmunizationComponent(StructuredBody sb)
        {
            Section section = new Section();

            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.2.1"));

            section.Code = new CE<string>(
                "11369-6",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "History of immunizations",
                null);

            section.Title = new ST("IMMUNIZATIONS");

            section.Text = new ED();
            section.Text = "<list listType=\"ordered\"><item><content ID=\"immunization1\">Influenza, seasonal</content></item></list>";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Entry e = new Entry();
            e.SetClinicalStatement(MakeSubstanceAdministration("IMMUNIZATIONS"));

            section.Entry = new List<Entry>();
            section.Entry.Add(e);

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
        }

        private static void AddVitalSignsComponent(StructuredBody sb)
        {
            Section section = new Section();
            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.4"));

            section.Code = new CE<string>(
                "8716-3",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "VITAL SIGNS",
                null);

            section.Title = new ST("VITAL SIGNS");

            section.Text = new ED("Vitals Normal:<list listType=\"ordered\"> <item><content ID=\"vit1\">Height - 70 in</content></item> <item><content ID=\"vit2\">Weight - 220 lb_en</content></item> </list>");
            section.Text.MediaType = "text/x-hl7-text+xml";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
        }

        private static void AddAdvancedDirectivesComponent(StructuredBody sb)
        {
            Section section = new Section();
            section.TemplateId = new LIST<II>();
            section.TemplateId.Add(new II("2.16.840.1.113883.10.20.22.2.21"));

            section.Code = new CE<string>(
                "42348-3",
                "2.16.840.1.113883.6.1",
                "LOINC",
                null,
                "Advance Directives",
                null);

            section.Title = new ST("ADVANCE DIRECTIVES");

            section.Text = new ED();
            section.Text = "No advance directives exist for this patient.";
            section.Text.Representation = EncapsulatedDataRepresentation.XML;
            section.Text.MediaType = "text/x-hl7-text+xml";

            Component3 comp3 = new Component3(ActRelationshipHasComponent.HasComponent, true);
            comp3.Section = section;

            sb.Component.Add(comp3);
        }

    }

    /// <summary>
    /// Patient with multiple race codes
    /// </summary>
    [Structure(Model = "POCD_MT000040", Name = "Patient", StructureType = StructureAttribute.StructureAttributeType.MessageType)]
    public class MyPatientMultipleRaceCodes : Patient
    {
        /// <summary>
        /// Race code 
        /// </summary>
        [Property(Name = "raceCode", PropertyType = PropertyAttribute.AttributeAttributeType.NonStructural)]
        public new SET<CE<String>> RaceCode { get; set; }

    }
}
