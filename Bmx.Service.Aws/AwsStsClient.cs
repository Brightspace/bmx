using System.Threading;
using System.Threading.Tasks;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace Bmx.Service.Aws {
	internal class AwsStsClient : IAwsStsClient {
		private readonly AmazonSecurityTokenServiceClient _stsClient;

		public AwsStsClient() {
			_stsClient = new AmazonSecurityTokenServiceClient();
		}

		public Task<AssumeRoleWithSAMLResponse> AssumeRoleWithSAMLAsync( AssumeRoleWithSAMLRequest request,
			CancellationToken cancellationToken = default(CancellationToken) ) {
			return _stsClient.AssumeRoleWithSAMLAsync( request, cancellationToken );
		}
	}
}
