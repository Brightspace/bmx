using System.Threading;
using System.Threading.Tasks;
using Amazon.SecurityToken.Model;

namespace Bmx.Service.Aws
{
	public interface IAwsStsClient
	{
		Task<AssumeRoleWithSAMLResponse> AssumeRoleWithSAMLAsync( AssumeRoleWithSAMLRequest request,
			CancellationToken cancellationToken = default(CancellationToken) );
	}
}
