// Program..: QueryBuilderExtensions.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 28/10/2024
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

namespace gwTibber.Classes;

public static class QueryBuilderExtensions
{
	public static TibberQueryBuilder WithHomesAndSubscriptions(this TibberQueryBuilder builder) =>
		  builder.WithAllScalarFields()
				 .WithViewer(
						 new ViewerQueryBuilder()
								.WithAllScalarFields()
								.WithAccountType()
								.WithHomes(
									  new HomeQueryBuilder()
											  .WithAllScalarFields()
											  .WithAddress(new AddressQueryBuilder().WithAllFields())
											  .WithCurrentSubscription(
													 new SubscriptionQueryBuilder()
															.WithAllScalarFields()
															.WithSubscriber(new LegalEntityQueryBuilder().WithAllFields())
															.WithPriceInfo(new PriceInfoQueryBuilder().WithCurrent(new PriceQueryBuilder().WithAllFields()))
											  )
											  .WithOwner(new LegalEntityQueryBuilder().WithAllFields())
											  .WithFeatures(new HomeFeaturesQueryBuilder().WithAllFields())
											  .WithMeteringPointData(new MeteringPointDataQueryBuilder().WithAllFields())
								)
				 );

	public static TibberQueryBuilder WithHomes(this TibberQueryBuilder builder) =>
		  builder.WithAllScalarFields()
				 .WithViewer(
						 new ViewerQueryBuilder()
								.WithAllScalarFields()
								.WithHomes(
									  new HomeQueryBuilder()
											  .WithAllScalarFields()
											  .WithFeatures(new HomeFeaturesQueryBuilder()
													 .WithAllFields())
								)
				 );

	public static TibberQueryBuilder WithHomeById(this TibberQueryBuilder builder, Guid homeId) =>
		  builder.WithAllScalarFields()
				 .WithViewer(
						 new ViewerQueryBuilder()
								.WithAllScalarFields()
								.WithHome(
									  new HomeQueryBuilder()
											  .WithAllScalarFields()
											  .WithFeatures(new HomeFeaturesQueryBuilder()
													 .WithAllFields()),
									  homeId
								)
				 );

	public static TibberQueryBuilder WithHomeConsumption(this TibberQueryBuilder builder, Guid homeId, EnergyResolution resolution, int? lastEntries) =>
		  builder.WithViewer(
						 new ViewerQueryBuilder()
								.WithHome(
									  new HomeQueryBuilder().WithConsumption(resolution, lastEntries ?? LastConsumptionEntries(resolution)),
									  homeId
								)
				 );

	public static TibberQueryBuilder WithHomeProduction(this TibberQueryBuilder builder, Guid homeId, EnergyResolution resolution, int? lastEntries) =>
		  builder.WithViewer(
				 new ViewerQueryBuilder()
						 .WithHome(
								new HomeQueryBuilder().WithHomeProduction(resolution, lastEntries ?? LastConsumptionEntries(resolution)),
								homeId
						 )
		  );

	public static HomeQueryBuilder WithConsumption(this HomeQueryBuilder homeQueryBuilder, EnergyResolution resolution, int lastEntries) =>
		  homeQueryBuilder.WithConsumption(
				 new HomeConsumptionConnectionQueryBuilder().WithNodes(new ConsumptionEntryQueryBuilder().WithAllFields()),
				 resolution,
				 last: lastEntries);

	public static HomeQueryBuilder WithHomeProduction(this HomeQueryBuilder homeQueryBuilder, EnergyResolution resolution, int lastEntries) =>
		  homeQueryBuilder.WithProduction(
				 new HomeProductionConnectionQueryBuilder().WithNodes(new ProductionEntryQueryBuilder().WithAllFields()),
				 resolution,
				 last: lastEntries);

	private static int LastConsumptionEntries(EnergyResolution resolution) =>
		  resolution switch
		  {
			  EnergyResolution.Annual => 1,
			  EnergyResolution.Daily => 30,
			  EnergyResolution.Hourly => 24,
			  EnergyResolution.Monthly => 12,
			  EnergyResolution.Weekly => 4,
			  _ => throw new NotSupportedException($"{resolution} resolution not supported")
		  };
}
